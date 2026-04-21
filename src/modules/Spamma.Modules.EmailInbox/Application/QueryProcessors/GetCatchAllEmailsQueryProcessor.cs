using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class GetCatchAllEmailsQueryProcessor(IDocumentSession documentSession)
    : IQueryProcessor<GetCatchAllEmailsQuery, GetCatchAllEmailsQueryResult>
{
    public async Task<QueryResult<GetCatchAllEmailsQueryResult>> Handle(GetCatchAllEmailsQuery request, CancellationToken cancellationToken)
    {
        var catchAllSubdomainId = EmailInboxSettingsDocument.CatchAllSubdomainId;

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));
        var skip = (page - 1) * pageSize;

        var emails = await documentSession.Query<EmailLookup>()
            .Where(x => x.SubdomainId == catchAllSubdomainId && x.DeletedAt == null)
            .OrderByDescending(x => x.SentAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(token: cancellationToken);

        var totalCount = await documentSession.Query<EmailLookup>()
            .CountAsync(x => x.SubdomainId == catchAllSubdomainId && x.DeletedAt == null, cancellationToken);

        var groups = emails
            .GroupBy(e =>
            {
                var fromAddress = e.EmailAddresses.FirstOrDefault(a => a.EmailAddressType == EmailAddressType.From)?.Address ?? string.Empty;
                var atIndex = fromAddress.IndexOf('@');
                return atIndex >= 0 ? fromAddress[(atIndex + 1)..] : string.Empty;
            })
            .Select(g => new GetCatchAllEmailsQueryResult.DomainGroup(
                g.Key,
                g.Select(e =>
                {
                    var toAddress = e.EmailAddresses.FirstOrDefault(a => a.EmailAddressType == EmailAddressType.To)?.Address ?? string.Empty;
                    return new GetCatchAllEmailsQueryResult.EmailSummary(e.Id, e.Subject, toAddress, e.SentAt, e.IsFavorite);
                }).ToList()))
            .ToList();

        return QueryResult<GetCatchAllEmailsQueryResult>.Succeeded(
            new GetCatchAllEmailsQueryResult(groups, totalCount));
    }
}
