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
        var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
        var commander = scope.ServiceProvider.GetRequiredService<ICommander>();
        var subdomainCache = scope.ServiceProvider.GetRequiredService<ISubdomainCache>();
        var chaosAddressCache = scope.ServiceProvider.GetRequiredService<IChaosAddressCache>();

        using var stream = new MemoryStream();

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

        // First-match semantics: iterate recipients in delivery order and return the configured SMTP response
        foreach (var recipient in recipients)
        {
            var recipientDomain = recipient.Domain.ToLowerInvariant();

            var subdomainMaybe = await subdomainCache.GetSubdomainAsync(recipientDomain, cancellationToken: cancellationToken);
            if (!subdomainMaybe.HasValue)
            {
                // No active subdomain for this recipient, continue to next recipient
                continue;
            }

            var subdomain = subdomainMaybe.Value;

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

        var messageId = Guid.NewGuid();
        var saveFileResult = await messageStoreProvider.StoreMessageContentAsync(messageId, message, cancellationToken);
        if (!saveFileResult.IsSuccess)
        {
            return SmtpResponse.TransactionFailed;
        }

        var addresses = message.To.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.To)).ToList();
        addresses.AddRange(message.Cc.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.Cc)));
        addresses.AddRange(message.Bcc.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.Bcc)));
        addresses.AddRange(message.From.Mailboxes.Select(x => new ReceivedEmailCommand.EmailAddress(x.Address, x.Name, EmailAddressType.From)));

        var saveDataResult = await commander.Send(
            new ReceivedEmailCommand(
                messageId,
                foundSubdomain.ParentDomainId,
                foundSubdomain.SubdomainId,
                message.Subject,
                message.Date.DateTime,
                addresses), cancellationToken);

        if (saveDataResult.Status != CommandResultStatus.Failed)
        {
            // Get background job channels (singleton registered in DI)
            var campaignChannel = scope.ServiceProvider.GetRequiredService<Channel<Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.CampaignCaptureJob>>();
            var chaosChannel = scope.ServiceProvider.GetRequiredService<Channel<Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.ChaosAddressReceivedJob>>();

            // Queue campaign capture job if header present (non-blocking, returns immediately)
            var campaignHeader = message.Headers.FirstOrDefault(x => x.Field.Equals("x-spamma-camp", StringComparison.InvariantCultureIgnoreCase));
            if (campaignHeader != null && !string.IsNullOrEmpty(campaignHeader.Value))
            {
                var campaignValue = campaignHeader.Value.Trim().ToLowerInvariant();
                var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown";
                var toAddress = message.To.Mailboxes.FirstOrDefault()?.Address ?? "unknown";

                var job = new Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.CampaignCaptureJob(
                    foundSubdomain.ParentDomainId,
                    foundSubdomain.SubdomainId,
                    messageId,
                    campaignValue,
                    message.Subject ?? string.Empty,
                    fromAddress,
                    toAddress,
                    DateTimeOffset.UtcNow);

                try
                {
                    // Queue for background processing - this never blocks for long
                    await campaignChannel.Writer.WriteAsync(job, cancellationToken);
                }
                catch (ChannelClosedException ex)
                {
                    logger.LogWarning(ex, "Campaign job channel closed, background processor may have stopped");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to queue campaign capture job for email {EmailId}", messageId);
                }
            }

            // Queue chaos address job if applicable (non-blocking, returns immediately)
            if (chaosAddressId.HasValue)
            {
                var job = new Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs.ChaosAddressReceivedJob(chaosAddressId.Value, DateTime.UtcNow);

                try
                {
                    // Queue for background processing - this never blocks for long
                    await chaosChannel.Writer.WriteAsync(job, cancellationToken);
                }
                catch (ChannelClosedException ex)
                {
                    logger.LogWarning(ex, "Chaos address job channel closed, background processor may have stopped");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to queue chaos address job for {ChaosAddressId}", chaosAddressId);
                }
            }

            return SmtpResponse.Ok;
        }

        await messageStoreProvider.DeleteMessageContentAsync(messageId, cancellationToken);
        return SmtpResponse.TransactionFailed;
    }
}