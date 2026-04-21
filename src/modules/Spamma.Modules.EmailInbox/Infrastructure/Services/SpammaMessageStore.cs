using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class SpammaMessageStore(PushNotificationManager pushNotificationManager, IOptions<EmailInboxSettings>? settings = null) : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        var scope = context.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SpammaMessageStore>>();
        var backgroundTaskQueue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();

        var memoryStream = new MemoryStream((int)buffer.Length);
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

        var messageId = Guid.NewGuid();

        var incomingPort = context.EndpointDefinition?.Endpoint?.Port ?? 0;
        var emailInboxSettingsOptions = settings ?? scope.ServiceProvider.GetService<IOptions<EmailInboxSettings>>();
        if (emailInboxSettingsOptions?.Value?.CatchAllPortEnabled == true && incomingPort == emailInboxSettingsOptions.Value.CatchAllPort)
        {
            backgroundTaskQueue.QueueBackgroundWorkItem(
                new StandardEmailCaptureJob(memoryStream, CatchAllConstants.DomainId, CatchAllConstants.SubdomainId, messageId));

            await pushNotificationManager.NotifyEmailAsync(
                new PushNotificationManager.EmailDetails(
                    messageId,
                    CatchAllConstants.SubdomainId,
                    message.From?.ToString() ?? string.Empty,
                    recipients.FirstOrDefault()?.Address ?? string.Empty,
                    message.Subject ?? string.Empty,
                    message.TextBody ?? message.HtmlBody ?? string.Empty,
                    DateTimeOffset.Now),
                cancellationToken);

            return SmtpResponse.Ok;
        }

        var subdomainCache = scope.ServiceProvider.GetRequiredService<ISubdomainCache>();
        var chaosAddressCache = scope.ServiceProvider.GetRequiredService<IChaosAddressCache>();

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
                backgroundTaskQueue.QueueBackgroundWorkItem(new ChaosEmailCaptureJob(
                    memoryStream,
                    chaosAddress.Value.DomainId,
                    chaosAddress.Value.SubdomainId,
                    chaosAddress.Value.ChaosAddressId));
                return new SmtpResponse((SmtpReplyCode)(int)code, code.ToString());
            }

            break; // Found valid subdomain, accept email
        }

        if (foundValidSubdomain == null)
        {
            var catchAllSettings = scope.ServiceProvider.GetService<IEmailInboxSettings>();

            if (catchAllSettings?.CatchAllModeEnabled != true)
            {
                logger.LogWarning("Email rejected - no valid subdomain for recipients");
                return SmtpResponse.MailboxNameNotAllowed;
            }

            logger.LogInformation("Catch-all mode active - accepting email for unregistered domain");

            var catchAllMessageId = Guid.NewGuid();
            var messageStoreProvider = scope.ServiceProvider.GetService<IMessageStoreProvider>();

            if (messageStoreProvider != null)
            {
                var storeResult = await messageStoreProvider.StoreMessageContentAsync(catchAllMessageId, message, cancellationToken);
                if (!storeResult.IsSuccess)
                {
                    return SmtpResponse.TransactionFailed;
                }
            }

            try
            {
                backgroundTaskQueue.QueueBackgroundWorkItem(new CatchAllEmailCaptureJob(
                    memoryStream,
                    EmailInboxSettingsDocument.CatchAllDomainId,
                    EmailInboxSettingsDocument.CatchAllSubdomainId,
                    catchAllMessageId));
            }
            catch
            {
                if (messageStoreProvider != null)
                {
                    await messageStoreProvider.DeleteMessageContentAsync(catchAllMessageId, cancellationToken);
                }

                return SmtpResponse.TransactionFailed;
            }

            return SmtpResponse.Ok;
        }

        if (string.IsNullOrWhiteSpace(campaignHeader))
        {
            backgroundTaskQueue.QueueBackgroundWorkItem(new StandardEmailCaptureJob(memoryStream, foundValidSubdomain.DomainId, foundValidSubdomain.SubdomainId, messageId));
        }
        else
        {
            // Campaign handling
        }

        // Notify push integrations
        await pushNotificationManager.NotifyEmailAsync(
            new PushNotificationManager.EmailDetails(
                messageId,
                foundValidSubdomain.SubdomainId,
                message.From?.ToString() ?? string.Empty,
                recipients.FirstOrDefault()?.Address ?? string.Empty,
                message.Subject ?? string.Empty,
                message.TextBody ?? message.HtmlBody ?? string.Empty,
                DateTimeOffset.Now),
            cancellationToken);

        return SmtpResponse.Ok;
    }
}