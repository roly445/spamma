using System.Buffers;
using System.Threading.Channels;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Infrastructure.Caching;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class SpammaMessageStore : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        return await SaveAsyncWithProvider(context.ServiceProvider, buffer, cancellationToken);
    }

    internal static async Task<SmtpResponse> SaveAsyncWithProvider(IServiceProvider provider, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SpammaMessageStore>>();
        var subdomainCache = scope.ServiceProvider.GetRequiredService<ISubdomainCache>();
        var chaosAddressCache = scope.ServiceProvider.GetRequiredService<IChaosAddressCache>();
        var ingestionChannel = scope.ServiceProvider.GetRequiredService<Channel<Infrastructure.Services.BackgroundJobs.EmailIngestionJob>>();

        // OPTIMIZATION: Pre-size MemoryStream to avoid reallocations
        using var stream = new MemoryStream((int)buffer.Length);

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            stream.Write(memory.Span);
        }

        stream.Position = 0;

        var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);

        // Build recipients in delivery order: To, Cc, Bcc
        var recipients = new List<MimeKit.MailboxAddress>();
        recipients.AddRange(message.To.Mailboxes);
        recipients.AddRange(message.Cc.Mailboxes);
        recipients.AddRange(message.Bcc.Mailboxes);

        SearchSubdomainsQueryResult.SubdomainSummary? foundSubdomain = null;
        Guid? chaosAddressId = null;

        // OPTIMIZATION: Extract unique domains FIRST, then batch cache lookups
        var uniqueDomains = recipients
            .Select(r => r.Domain.ToLowerInvariant())
            .Distinct()
            .ToList();

        // Fetch all subdomains concurrently
        // Cache has stampede protection - first request queries DB, others wait for cache population
        var subdomainTasks = uniqueDomains
            .Select(async domain => new { Domain = domain, Subdomain = await subdomainCache.GetSubdomainAsync(domain, forceRefresh: false, cacheOnly: false, cancellationToken: cancellationToken) })
            .ToList();

        var subdomainResults = await Task.WhenAll(subdomainTasks);
        var subdomainMap = subdomainResults
            .Where(r => r.Subdomain.HasValue)
            .ToDictionary(r => r.Domain, r => r.Subdomain.Value);

        // First-match semantics: iterate recipients in delivery order and return the configured SMTP response
        foreach (var recipient in recipients)
        {
            var recipientDomain = recipient.Domain.ToLowerInvariant();

            if (!subdomainMap.TryGetValue(recipientDomain, out var subdomain))
            {
                // No active subdomain for this recipient, continue to next recipient
                continue;
            }

            // keep the first discovered subdomain for later ReceivedEmailCommand if no chaos address matches
            foundSubdomain ??= subdomain;

            // Check ChaosAddress by calling cache for this subdomain and local-part
            var localPart = recipient.Address.Split('@')[0];
            var chaosAddressMaybe = await chaosAddressCache.GetChaosAddressAsync(subdomain.SubdomainId, localPart, cancellationToken: cancellationToken);
            if (chaosAddressMaybe.HasValue)
            {
                var chaos = chaosAddressMaybe.Value;

                if (!chaos.Enabled)
                {
                    continue;
                }

                chaosAddressId = chaos.ChaosAddressId;

                // Map configured SmtpResponseCode -> numeric reply code and return
                var code = (int)chaos.ConfiguredSmtpCode;

                return new SmtpResponse((SmtpServer.Protocol.SmtpReplyCode)code, chaos.ConfiguredSmtpCode.ToString());
            }
        }

        if (foundSubdomain == null)
        {
            return SmtpResponse.MailboxNameNotAllowed;
        }

        // Check if this is a campaign email (has x-spamma-camp header)
        var campaignHeader = message.Headers.FirstOrDefault(x => x.Field.Equals("x-spamma-camp", StringComparison.InvariantCultureIgnoreCase));
        var isCampaignEmail = campaignHeader != null && !string.IsNullOrEmpty(campaignHeader.Value);

        var messageId = Guid.NewGuid();

        // CRITICAL: For non-campaign emails, write file SYNCHRONOUSLY before returning OK
        // This ensures email is durable even if server crashes before background processing
        // Trade-off: Blocks SMTP for ~5-20ms (file I/O), but prevents data loss
        if (!isCampaignEmail)
        {
            var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
            var saveFileResult = await messageStoreProvider.StoreMessageContentAsync(messageId, message, cancellationToken);
            if (!saveFileResult.IsSuccess)
            {
                logger.LogError("Failed to store message file for {MessageId}", messageId);
                return SmtpResponse.TransactionFailed;
            }

            logger.LogDebug("Stored file for non-campaign email {MessageId}", messageId);
        }

        // Queue background job for command execution (DB write) and downstream jobs
        // Even if this fails or server crashes, file is safe on disk for non-campaign emails
        var ingestionJob = new Infrastructure.Services.BackgroundJobs.EmailIngestionJob(
            messageId,
            message,
            foundSubdomain.ParentDomainId,
            foundSubdomain.SubdomainId,
            chaosAddressId,
            isCampaignEmail);

        try
        {
            await ingestionChannel.Writer.WriteAsync(ingestionJob, cancellationToken);
            logger.LogDebug("Queued email ingestion job for message {MessageId} (Campaign: {IsCampaign})", messageId, isCampaignEmail);
            return SmtpResponse.Ok;
        }
        catch (ChannelClosedException ex)
        {
            logger.LogError(ex, "Email ingestion channel closed, background processor may have stopped");

            // If channel write fails for non-campaign email, we already saved the file
            // Background recovery will pick it up on restart
            return isCampaignEmail ? SmtpResponse.TransactionFailed : SmtpResponse.Ok;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to queue email ingestion job for message {MessageId}", messageId);

            // Same as above - file is safe, just log the queue failure
            return isCampaignEmail ? SmtpResponse.TransactionFailed : SmtpResponse.Ok;
        }
    }
}