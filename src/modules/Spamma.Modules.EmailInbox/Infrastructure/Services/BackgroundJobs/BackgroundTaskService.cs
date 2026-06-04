using BluQube.Commands;
using BluQube.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MimeKit;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public class BackgroundTaskService(
    IBackgroundTaskQueue taskQueue,
    IServiceProvider serviceProvider,
    PushNotificationManager pushNotificationManager) : BackgroundService
{
    internal static async Task ProcessWorkItemAsync(
        IBaseEmailCaptureJob workItem,
        ICommandRunner commander,
        IMessageStoreProvider messageStoreProvider,
        CancellationToken cancellationToken,
        PushNotificationManager? pushNotificationManager = null)
    {
        var messageId = Guid.NewGuid();

        try
        {
            workItem.MimeStream.Position = 0;

            var message = await MimeMessage.LoadAsync(workItem.MimeStream, cancellationToken);
            switch (workItem)
            {
                case CampaignCaptureJob:
                {
                    var campaignValue = message.Headers["x-spamma-camp"] ?? string.Empty;
                    var campaignMessageId = messageId;
                    var result = await commander.Send(
                        new RecordCampaignCaptureCommand(
                            workItem.DomainId,
                            workItem.SubdomainId,
                            campaignMessageId,
                            campaignValue,
                            message.Date), cancellationToken);

                    if (result.Status == CommandResultStatus.Succeeded)
                    {
                        if (result.Data.IsFirstEmail)
                        {
                            await ExtractEmailAddressesAndSendCommand(campaignMessageId, message, commander,
                                messageStoreProvider, workItem, result.Data.CampaignId,
                                cancellationToken: cancellationToken);
                        }

                        if (pushNotificationManager is not null)
                        {
                            await pushNotificationManager.NotifyEmailAsync(
                                new PushNotificationManager.EmailDetails(
                                    campaignMessageId,
                                    workItem.SubdomainId,
                                    message.From?.ToString() ?? string.Empty,
                                    message.To.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                                    message.Subject ?? string.Empty,
                                    message.TextBody ?? message.HtmlBody ?? string.Empty,
                                    DateTimeOffset.Now,
                                    result.Data.CampaignId,
                                    campaignValue),
                                cancellationToken);
                        }
                    }

                    break;
                }

                case ChaosEmailCaptureJob captureJob:
                    await commander.Send(
                        new RecordChaosAddressReceivedCommand(captureJob.ChaosAddressId, message.Date),
                        cancellationToken);
                    break;
                case CatchAllEmailCaptureJob catchAllJob:
                    Guid? campaignId = null;
                    if (!string.IsNullOrWhiteSpace(catchAllJob.CampaignValue))
                    {
                        var campaignCaptureResult = await commander.Send(
                            new RecordCampaignCaptureCommand(
                                catchAllJob.DomainId,
                                catchAllJob.SubdomainId,
                                catchAllJob.MessageId,
                                catchAllJob.CampaignValue,
                                message.Date),
                            cancellationToken);

                        if (campaignCaptureResult.Status == CommandResultStatus.Succeeded)
                        {
                            campaignId = campaignCaptureResult.Data.CampaignId;
                        }
                    }

                    var saved = await ExtractEmailAddressesAndSendCommand(catchAllJob.MessageId, message, commander,
                        messageStoreProvider, workItem, campaignId,
                        isCatchAll: true,
                        catchAllSenderAddressId: catchAllJob.CatchAllSenderAddressId,
                        cancellationToken: cancellationToken);

                    if (saved && pushNotificationManager is not null)
                    {
                        await pushNotificationManager.NotifyEmailAsync(
                            new PushNotificationManager.EmailDetails(
                                catchAllJob.MessageId,
                                catchAllJob.SubdomainId,
                                message.From?.ToString() ?? string.Empty,
                                message.To.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                                message.Subject ?? string.Empty,
                                message.TextBody ?? message.HtmlBody ?? string.Empty,
                                DateTimeOffset.Now,
                                campaignId,
                                catchAllJob.CampaignValue,
                                IsCatchAll: true),
                            cancellationToken);
                    }

                    break;
                case StandardEmailCaptureJob standardJob:
                    await ExtractEmailAddressesAndSendCommand(standardJob.MessageId, message, commander,
                        messageStoreProvider, workItem, cancellationToken: cancellationToken);
                    break;
            }
        }
        catch (Exception)
        {
            // Log the exception if necessary
        }
        finally
        {
            await workItem.MimeStream.DisposeAsync();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var commander = scope.ServiceProvider.GetRequiredService<ICommandRunner>();
        var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await taskQueue.DequeueAsync(stoppingToken);
            await ProcessWorkItemAsync(workItem, commander, messageStoreProvider, stoppingToken, pushNotificationManager);
        }
    }

    private static async Task<bool> ExtractEmailAddressesAndSendCommand(
        Guid messageId, MimeMessage message, ICommandRunner commander,
        IMessageStoreProvider messageStoreProvider,
        IBaseEmailCaptureJob workItem, Guid? campaignId = null, bool isCatchAll = false,
        Guid? catchAllSenderAddressId = null, CancellationToken cancellationToken = default)
    {
        var storeResult = await messageStoreProvider.StoreMessageContentAsync(messageId, message, cancellationToken);
        if (!storeResult.IsSuccess)
        {
            return false;
        }

        var addresses = message.To.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name ?? string.Empty, EmailAddressType.To))
            .ToList();
        addresses.AddRange(message.Cc.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name ?? string.Empty, EmailAddressType.Cc)));
        addresses.AddRange(message.Bcc.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name ?? string.Empty, EmailAddressType.Bcc)));
        addresses.AddRange(message.From.Mailboxes
            .Select(x => new EmailAddress(x.Address, x.Name ?? string.Empty, EmailAddressType.From)));

        CommandResult commandResult;
        if (campaignId == null)
        {
            commandResult = await commander.Send(
                new ReceivedEmailCommand(
                    messageId,
                    workItem.DomainId,
                    workItem.SubdomainId,
                    message.Subject ?? string.Empty,
                    message.Date,
                    addresses,
                    isCatchAll,
                    isCatchAll ? workItem.DomainId : null,
                    catchAllSenderAddressId), cancellationToken);
        }
        else
        {
            commandResult = await commander.Send(
                new CampaignEmailReceivedCommand(
                    messageId,
                    workItem.DomainId,
                    workItem.SubdomainId,
                    message.Subject ?? string.Empty,
                    message.Date,
                    campaignId.Value,
                    addresses,
                    catchAllSenderAddressId), cancellationToken);
        }

        if (commandResult.Status != CommandResultStatus.Succeeded)
        {
            await messageStoreProvider.DeleteMessageContentAsync(messageId, cancellationToken);
            return false;
        }

        return true;
    }
}
