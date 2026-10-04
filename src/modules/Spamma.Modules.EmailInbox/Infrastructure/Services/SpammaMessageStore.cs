using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Caching;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class SpammaMessageStore : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SpammaMessageStore>>();
        var subdomainCache = scope.ServiceProvider.GetRequiredService<ISubdomainCache>();
        var chaosAddressCache = scope.ServiceProvider.GetRequiredService<IChaosAddressCache>();
        var backgroundTaskQueue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();
        var catchAllSenderAddressCache = scope.ServiceProvider.GetRequiredService<ICatchAllSenderAddressCache>();

        using var memoryStream = new MemoryStream((int)buffer.Length);
        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            memoryStream.Write(memory.Span);
        }

        memoryStream.Position = 0;
        var message = await MimeKit.MimeMessage.LoadAsync(memoryStream, cancellationToken);
        var headers = message.Headers;

        var campaignHeader = headers["x-spamma-camp"];
        var recipients = new List<MimeKit.MailboxAddress>();
        if (headers[MimeKit.HeaderId.To] != null && MimeKit.InternetAddressList.TryParse(headers[MimeKit.HeaderId.To], out var toList))
        {
            recipients.AddRange(toList.Mailboxes);
        }

        ISubdomainCache.CachedSubdomain? foundValidSubdomain = null;

        foreach (var recipient in recipients)
        {
            var domain = recipient.Domain.ToLowerInvariant();
            var localPart = recipient.Address.Split('@')[0].ToLowerInvariant();
            var subdomain = await subdomainCache.GetSubdomainAsync(domain, forceRefresh: false, cancellationToken: cancellationToken);

            if (!subdomain.HasValue)
            {
                continue;
            }

            foundValidSubdomain = subdomain.Value;

            var chaosAddress = await chaosAddressCache.GetChaosAddressAsync(
                subdomain.Value.SubdomainId,
                localPart,
                forceRefresh: false,
                cancellationToken: cancellationToken);

            if (chaosAddress.HasValue)
            {
                var code = chaosAddress.Value.ConfiguredSmtpCode;
                if (!TryQueue(backgroundTaskQueue, new ChaosEmailCaptureJob(
                    memoryStream,
                    chaosAddress.Value.DomainId,
                    chaosAddress.Value.SubdomainId,
                    chaosAddress.Value.ChaosAddressId), logger))
                {
                    return new SmtpResponse(SmtpReplyCode.Aborted, "Temporary storage failure");
                }

                return new SmtpResponse((SmtpReplyCode)(int)code, code.ToString());
            }

            break; // Found valid subdomain, accept email
        }

        if (foundValidSubdomain == null)
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<IEmailInboxSettingsService>();
            var catchAllEnabled = await settingsService.GetCatchAllModeEnabledAsync(cancellationToken);

            if (!catchAllEnabled)
            {
                logger.LogWarning("Email rejected - no valid subdomain for recipients");
                return SmtpResponse.MailboxNameNotAllowed;
            }

            logger.LogInformation("Catch-all mode active - accepting email for unregistered domain");

            var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                logger.LogWarning("Catch-all mode: email rejected - no From address present");
                return SmtpResponse.MailboxNameNotAllowed;
            }

            var cachedSender = await catchAllSenderAddressCache.GetSenderAddressAsync(fromAddress, false, cancellationToken);
            if (cachedSender == null)
            {
                logger.LogWarning("Catch-all mode: sender {FromAddress} is not whitelisted", fromAddress);
                return SmtpResponse.MailboxNameNotAllowed;
            }

            var catchAllMessageId = Guid.NewGuid();
            if (!TryQueue(backgroundTaskQueue, new CatchAllEmailCaptureJob(
                memoryStream,
                CatchAllConstants.DomainId,
                CatchAllConstants.SubdomainId,
                catchAllMessageId,
                cachedSender.SenderAddressId,
                campaignHeader), logger))
            {
                return new SmtpResponse(SmtpReplyCode.Aborted, "Temporary storage failure");
            }

            return SmtpResponse.Ok;
        }

        var messageId = Guid.NewGuid();

        if (string.IsNullOrWhiteSpace(campaignHeader))
        {
            if (!TryQueue(backgroundTaskQueue, new StandardEmailCaptureJob(memoryStream, foundValidSubdomain.DomainId, foundValidSubdomain.SubdomainId, messageId), logger))
            {
                return new SmtpResponse(SmtpReplyCode.Aborted, "Temporary storage failure");
            }
        }
        else
        {
            if (!TryQueue(backgroundTaskQueue, new CampaignCaptureJob(memoryStream, foundValidSubdomain.DomainId, foundValidSubdomain.SubdomainId, messageId), logger))
            {
                return new SmtpResponse(SmtpReplyCode.Aborted, "Temporary storage failure");
            }
        }

        return SmtpResponse.Ok;
    }

    private static bool TryQueue(IBackgroundTaskQueue queue, IBaseEmailCaptureJob job, ILogger<SpammaMessageStore> logger)
    {
        try
        {
            queue.QueueBackgroundWorkItem(job);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP capture was not durably queued for domain {DomainId}, subdomain {SubdomainId}", job.DomainId, job.SubdomainId);
            EmailCaptureMetrics.QueueFailures.Add(1);
            return false;
        }
    }
}
