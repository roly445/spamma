using BluQube.Queries;
using Marten;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

internal class SearchCatchAllSenderAddressesQueryProcessor(IDocumentSession documentSession)
    : IQueryProcessor<SearchCatchAllSenderAddressesQuery, SearchCatchAllSenderAddressesQueryResult>
{
    public async Task<QueryResult<SearchCatchAllSenderAddressesQueryResult>> Handle(SearchCatchAllSenderAddressesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

        var totalCount = await documentSession.Query<CatchAllSenderAddressLookup>()
            .CountAsync(x => !x.IsRemoved, cancellationToken);

        var items = await documentSession.Query<CatchAllSenderAddressLookup>()
            .Where(x => !x.IsRemoved)
            .OrderByDescending(x => x.AddedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(token: cancellationToken);

        var summaries = items
            .Select(x => new SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary(
                x.Id,
                x.SenderAddress,
                x.AssignedUserIds.Count,
                x.IsRemoved,
                x.AddedAt))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return QueryResult<SearchCatchAllSenderAddressesQueryResult>.Succeeded(
            new SearchCatchAllSenderAddressesQueryResult(summaries, totalCount, page, pageSize, totalPages));
    }
}
