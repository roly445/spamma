using System.Buffers;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Modules.Common;
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
            var scope = context.ServiceProvider.CreateScope();
            var messageStoreProvider = scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>();
            var commander = scope.ServiceProvider.GetRequiredService<ICommander>();
            var querier = scope.ServiceProvider.GetRequiredService<IQuerier>();
            var tempObjectStore = scope.ServiceProvider.GetRequiredService<ITempObjectStore>();

            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                stream.Write(memory.Span);
            }

            stream.Position = 0;

            var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);

            var domains = message.To.Mailboxes.Select(x => x.Domain.ToLower()).ToList();
            domains.AddRange(message.Cc.Mailboxes.Select(x => x.Domain.ToLower()));
            domains.AddRange(message.Bcc.Mailboxes.Select(x => x.Domain.ToLower()));

            var uniqueDomains = domains.Distinct().ToList();

            SearchSubdomainsQueryResult.SubdomainSummary? foundSubdomain = null;
            foreach (var uniqueDomain in uniqueDomains)
            {
                var query = new SearchSubdomainsQuery(
                    SearchTerm: uniqueDomain,
                    ParentDomainId: null,
                    Status: SubdomainStatus.Active,
                    Page: 1,
                    PageSize: 1,
                    SortBy: "domainname",
                    SortDescending: false);
                tempObjectStore.AddReferenceForObject(query);
                var result = await querier.Send(query, cancellationToken);
                if (result.Status == QueryResultStatus.Succeeded && result.Data.TotalCount > 0)
                {
                    foundSubdomain = result.Data.Items[0];
                    break;
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
                return SmtpResponse.Ok;
            }

            await messageStoreProvider.DeleteMessageContentAsync(messageId, cancellationToken);
            return SmtpResponse.TransactionFailed;
        }
    }