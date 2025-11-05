using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

using Spamma.Modules.Common;
using Spamma.Modules.DomainManagement.Client.Application.Commands.RecordChaosAddressReceived;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
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
        var querier = scope.ServiceProvider.GetRequiredService<IQuerier>();

        // documentSession removed - use querier/commander for cross-module access
        var tempObjectStore = scope.ServiceProvider.GetRequiredService<IInternalQueryStore>();

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

        // First-match semantics: iterate recipients in delivery order and return the configured SMTP response
        foreach (var recipient in recipients)
        {
            var recipientDomain = recipient.Domain.ToLowerInvariant();
            var query = new SearchSubdomainsQuery(
                SearchTerm: recipientDomain,
                ParentDomainId: null,
                Status: SubdomainStatus.Active,
                Page: 1,
                PageSize: 1,
                SortBy: "domainname",
                SortDescending: false);
            tempObjectStore.AddReferenceForObject(query);
            var subdomainResult = await querier.Send(query, cancellationToken);
            if (subdomainResult.Status != QueryResultStatus.Succeeded || subdomainResult.Data.TotalCount == 0)
            {
                // No active subdomain for this recipient, continue to next recipient
                continue;
            }

            var subdomain = subdomainResult.Data.Items[0];

            // keep the first discovered subdomain for later ReceivedEmailCommand if no chaos address matches
            foundSubdomain ??= subdomain;

            // Check ChaosAddress by calling domain-management query for this subdomain and local-part
            var localPart = recipient.Address.Split('@')[0];
            var chaosQuery = new GetChaosAddressBySubdomainAndLocalPartQuery(subdomain.Id, localPart);
            tempObjectStore.AddReferenceForObject(chaosQuery);
            var chaosResult = await querier.Send(chaosQuery, cancellationToken);
            if (chaosResult is { Status: QueryResultStatus.Succeeded, Data.Enabled: true })
            {
                var chaos = chaosResult.Data;

                // best-effort record receive
                try
                {
                    // Use client-side command contract so EmailInbox doesn't reference DomainManagement server project
                    await commander.Send(new RecordChaosAddressReceivedCommand(chaos.Id, DateTime.UtcNow), cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to record chaos address receive for {ChaosAddressId}", chaos.Id);
                }

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
                foundSubdomain.Id,
                message.Subject,
                message.Date.DateTime,
                addresses), cancellationToken);

        if (saveDataResult.Status != CommandResultStatus.Failed)
        {
            // Detect and record campaign header if present
            var campaignHeader = message.Headers.FirstOrDefault(x => x.Field.Equals("x-spamma-camp", StringComparison.InvariantCultureIgnoreCase));
            if (campaignHeader != null && !string.IsNullOrEmpty(campaignHeader.Value))
            {
                var campaignValue = campaignHeader.Value.Trim().ToLowerInvariant();

                var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown";
                var toAddress = message.To.Mailboxes.FirstOrDefault()?.Address ?? "unknown";

                await commander.Send(
                    new RecordCampaignCaptureCommand(
                        foundSubdomain.Id,
                        messageId,
                        campaignValue,
                        message.Subject ?? string.Empty,
                        fromAddress,
                        toAddress,
                        DateTimeOffset.UtcNow),
                    cancellationToken);
            }

            return SmtpResponse.Ok;
        }

        await messageStoreProvider.DeleteMessageContentAsync(messageId, cancellationToken);
        return SmtpResponse.TransactionFailed;
    }
}