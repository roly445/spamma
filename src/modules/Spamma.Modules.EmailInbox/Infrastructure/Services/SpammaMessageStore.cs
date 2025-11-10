using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class SpammaMessageStore : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        var scope = context.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SpammaMessageStore>>();
        var subdomainCache = scope.ServiceProvider.GetRequiredService<ISubdomainCache>();
        var chaosAddressCache = scope.ServiceProvider.GetRequiredService<IChaosAddressCache>();
        var backgroundTaskQueue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();

        var memoryStream = new MemoryStream((int)buffer.Length);
        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            memoryStream.Write(memory.Span);
        }

        memoryStream.Position = 0;
        var parser = new MimeKit.MimeParser(memoryStream, MimeKit.MimeFormat.Entity);
        var headers = await parser.ParseHeadersAsync(cancellationToken);

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
            logger.LogWarning("Email rejected - no valid subdomain for recipients");
            return SmtpResponse.MailboxNameNotAllowed;
        }

        if (string.IsNullOrWhiteSpace(campaignHeader))
        {
            backgroundTaskQueue.QueueBackgroundWorkItem(new StandardEmailCaptureJob(memoryStream, foundValidSubdomain.DomainId, foundValidSubdomain.SubdomainId));
        }
        else
        {
            backgroundTaskQueue.QueueBackgroundWorkItem(new CampaignCaptureJob(memoryStream, foundValidSubdomain.DomainId, foundValidSubdomain.SubdomainId));
        }

        return SmtpResponse.Ok;
    }
}