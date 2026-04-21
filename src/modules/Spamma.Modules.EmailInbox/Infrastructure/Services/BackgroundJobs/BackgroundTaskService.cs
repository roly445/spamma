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
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var commander = scope.ServiceProvider.GetRequiredService<ICommander>();
        var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await taskQueue.DequeueAsync(stoppingToken);
            var messageId = Guid.NewGuid();

            try
            {
                workItem.MimeStream.Position = 0;

                var message = await MimeMessage.LoadAsync(workItem.MimeStream, stoppingToken);
                switch (workItem)
                {
                    case CampaignCaptureJob:
                    {
                        var campaignValue = message.Headers["x-spamma-camp"] ?? string.Empty;
                        var result = await commander.Send(
                            new RecordCampaignCaptureCommand(
                                workItem.DomainId,
                                workItem.SubdomainId,
                                messageId,
                                campaignValue,
                                message.Date), stoppingToken);

                        if (result is { Status: CommandResultStatus.Succeeded, Data.IsFirstEmail: true })
                        {
                            await ExtractEmailAddressesAndSendCommand(messageId, message, commander,
                                messageStoreProvider, workItem, result.Data.CampaignId,
                                cancellationToken: stoppingToken);
                        }

                        break;
                    }

                    case ChaosEmailCaptureJob captureJob:
                        await commander.Send(
                            new RecordChaosAddressReceivedCommand(captureJob.ChaosAddressId, message.Date),
                            stoppingToken);
                        break;
                    case CatchAllEmailCaptureJob catchAllJob:
                        await ExtractEmailAddressesAndSendCommand(catchAllJob.MessageId, message, commander,
                            messageStoreProvider, workItem, isCatchAll: true, cancellationToken: stoppingToken);
                        break;
                    case StandardEmailCaptureJob standardJob:
                        await ExtractEmailAddressesAndSendCommand(standardJob.MessageId, message, commander,
                            messageStoreProvider, workItem, cancellationToken: stoppingToken);
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
    }

    private static async Task ExtractEmailAddressesAndSendCommand(
        Guid messageId, MimeMessage message, ICommander commander,
        IMessageStoreProvider messageStoreProvider,
        IBaseEmailCaptureJob workItem, Guid? campaignId = null, bool isCatchAll = false, CancellationToken cancellationToken = default)
    {
        var storeResult = await messageStoreProvider.StoreMessageContentAsync(messageId, message, cancellationToken);
        if (!storeResult.IsSuccess)
        {
            return;
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
                    isCatchAll ? workItem.DomainId : null), cancellationToken);
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
                    addresses), cancellationToken);
        }

        if (commandResult.Status != CommandResultStatus.Succeeded)
        {
            await messageStoreProvider.DeleteMessageContentAsync(messageId, cancellationToken);
        }
    }
}