using BluQube.Commands;
using BluQube.Constants;
using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

public class BackgroundTaskService(
    IServiceScopeFactory scopeFactory,
    PushNotificationManager pushNotificationManager,
    ILogger<BackgroundTaskService> logger) : ICapSubscribe
{
    [CapSubscribe(BackgroundTaskQueue.CaptureTopic)]
    public async Task ProcessAsync(EmailCaptureEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var commander = scope.ServiceProvider.GetRequiredService<ICommandRunner>();
        var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        await ProcessWorkItemAsync(
            envelope.ToJob(), commander, messageStoreProvider, cancellationToken,
            pushNotificationManager, logger, emailRepository);
    }

    internal static async Task ProcessWorkItemAsync(
        IBaseEmailCaptureJob workItem,
        ICommandRunner commander,
        IMessageStoreProvider messageStoreProvider,
        CancellationToken cancellationToken,
        PushNotificationManager? pushNotificationManager = null,
        ILogger<BackgroundTaskService>? logger = null,
        IEmailRepository? emailRepository = null)
    {
        var messageId = workItem switch
        {
            StandardEmailCaptureJob standard => standard.MessageId,
            CatchAllEmailCaptureJob catchAll => catchAll.MessageId,
            CampaignCaptureJob campaign when campaign.MessageId != Guid.Empty => campaign.MessageId,
            ChaosEmailCaptureJob chaos when chaos.MessageId != Guid.Empty => chaos.MessageId,
            _ => Guid.NewGuid(),
        };

        try
        {
            workItem.MimeStream.Position = 0;

            var message = await MimeMessage.LoadAsync(workItem.MimeStream, cancellationToken);
            if (emailRepository is not null && workItem is StandardEmailCaptureJob or CatchAllEmailCaptureJob or CampaignCaptureJob)
            {
                var existingMessageId = workItem switch
                {
                    StandardEmailCaptureJob standard => standard.MessageId,
                    CatchAllEmailCaptureJob catchAll => catchAll.MessageId,
                    _ => messageId,
                };
                var existing = await emailRepository.GetByIdAsync(existingMessageId, cancellationToken);
                if (existing.HasValue)
                {
                    return;
                }
            }

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
                            var sampleSaved = await ExtractEmailAddressesAndSendCommand(campaignMessageId, message, commander,
                                messageStoreProvider, workItem, result.Data.CampaignId,
                                cancellationToken: cancellationToken);
                            if (!sampleSaved)
                            {
                                throw new InvalidOperationException($"Could not persist campaign sample email {campaignMessageId}.");
                            }
                        }

                        if (pushNotificationManager is not null)
                        {
                            await NotifySafelyAsync(
                                pushNotificationManager,
                                new PushNotificationManager.EmailDetails(
                                    campaignMessageId,
                                    workItem.SubdomainId,
                                    message.From?.ToString() ?? string.Empty,
                                    message.To.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                                    message.Subject ?? string.Empty,
                                    message.TextBody ?? message.HtmlBody ?? string.Empty,
                                    DateTimeOffset.Now,
                                    result.Data.CampaignId,
                                    campaignValue,
                                    DomainId: workItem.DomainId),
                                logger, cancellationToken);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not record campaign capture {campaignMessageId}: {result.Status}.");
                    }

                    break;
                }

                case ChaosEmailCaptureJob captureJob:
                    var chaosResult = await commander.Send(
                        new RecordChaosAddressReceivedCommand(captureJob.ChaosAddressId, message.Date),
                        cancellationToken);
                    if (chaosResult.Status != CommandResultStatus.Succeeded)
                    {
                        throw new InvalidOperationException($"Could not record chaos address capture {captureJob.ChaosAddressId}: {chaosResult.Status}.");
                    }

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
                        else
                        {
                            throw new InvalidOperationException($"Could not record catch-all campaign capture {catchAllJob.MessageId}: {campaignCaptureResult.Status}.");
                        }
                    }

                    var saved = await ExtractEmailAddressesAndSendCommand(catchAllJob.MessageId, message, commander,
                        messageStoreProvider, workItem, campaignId,
                        isCatchAll: true,
                        catchAllSenderAddressId: catchAllJob.CatchAllSenderAddressId,
                        cancellationToken: cancellationToken);
                    if (!saved)
                    {
                        throw new InvalidOperationException($"Could not persist catch-all email {catchAllJob.MessageId}.");
                    }

                    if (pushNotificationManager is not null)
                    {
                        await NotifySafelyAsync(
                            pushNotificationManager,
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
                                IsCatchAll: true,
                                DomainId: catchAllJob.DomainId,
                                CatchAllSenderAddressId: catchAllJob.CatchAllSenderAddressId),
                            logger, cancellationToken);
                    }

                    break;
                case StandardEmailCaptureJob standardJob:
                    var standardSaved = await ExtractEmailAddressesAndSendCommand(standardJob.MessageId, message, commander,
                        messageStoreProvider, workItem, cancellationToken: cancellationToken);
                    if (!standardSaved)
                    {
                        throw new InvalidOperationException($"Could not persist email {standardJob.MessageId}.");
                    }

                    if (pushNotificationManager is not null)
                    {
                        await NotifySafelyAsync(
                            pushNotificationManager,
                            new PushNotificationManager.EmailDetails(
                                standardJob.MessageId,
                                standardJob.SubdomainId,
                                message.From?.ToString() ?? string.Empty,
                                message.To.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                                message.Subject ?? string.Empty,
                                message.TextBody ?? message.HtmlBody ?? string.Empty,
                                DateTimeOffset.Now,
                                DomainId: standardJob.DomainId),
                            logger, cancellationToken);
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "SMTP capture processing failed for {JobType}, domain {DomainId}, subdomain {SubdomainId}, message {MessageId}",
                workItem.GetType().Name, workItem.DomainId, workItem.SubdomainId, messageId);
            EmailCaptureMetrics.ProcessingFailures.Add(1);
            throw new InvalidOperationException($"SMTP capture processing failed for {workItem.GetType().Name}, message {messageId}.", ex);
        }
        finally
        {
            await workItem.MimeStream.DisposeAsync();
        }
    }

    private static async Task NotifySafelyAsync(
        PushNotificationManager manager,
        PushNotificationManager.EmailDetails details,
        ILogger<BackgroundTaskService>? logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await manager.NotifyEmailAsync(details, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Email {MessageId} was stored but its live notification failed", details.Id);
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
            return false;
        }

        return true;
    }
}
