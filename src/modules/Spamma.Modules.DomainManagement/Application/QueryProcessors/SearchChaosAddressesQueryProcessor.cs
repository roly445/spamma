using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class SearchChaosAddressesQueryProcessor(IDocumentSession session) : IQueryProcessor<SearchChaosAddressesQuery, SearchChaosAddressesQueryResult>
{
    public async Task<QueryResult<SearchChaosAddressesQueryResult>> Handle(SearchChaosAddressesQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<ChaosAddressLookup>();

        var filteredAndSorted = GetFilteredAndSortedQuery(baseQuery, request);

        var totalCount = await filteredAndSorted.CountAsync(cancellationToken);

        var items = await filteredAndSorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var summaries = items.Select(x => new ChaosAddressSummary(
            x.Id,
            x.SubdomainId,
            x.LocalPart,
            x.ConfiguredSmtpCode,
            x.Enabled,
            x.TotalReceived,
            x.LastReceivedAt,
            x.CreatedAt,
            x.CreatedBy)).ToList();

        var result = new SearchChaosAddressesQueryResult(summaries, totalCount, request.PageNumber, request.PageSize);
        return QueryResult<SearchChaosAddressesQueryResult>.Succeeded(result);
    }

    private static IQueryable<ChaosAddressLookup> GetFilteredAndSortedQuery(IQueryable<ChaosAddressLookup> query, SearchChaosAddressesQuery request)
    {
        var filtered = query;

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filtered = filtered.Where(x => x.LocalPart.ToLower().Contains(searchTerm));
        }

        if (request.SubdomainId.HasValue)
        {
            filtered = filtered.Where(x => x.SubdomainId == request.SubdomainId.Value);
        }

        if (request.Enabled.HasValue)
        {
            filtered = filtered.Where(x => x.Enabled == request.Enabled.Value);
        }

        return request.SortBy switch
        {
            "LocalPart" => request.SortDescending
                ? filtered.OrderByDescending(x => x.LocalPart)
                : filtered.OrderBy(x => x.LocalPart),
            "Enabled" => request.SortDescending
                ? filtered.OrderByDescending(x => x.Enabled)
                : filtered.OrderBy(x => x.Enabled),
            "TotalReceived" => request.SortDescending
                ? filtered.OrderByDescending(x => x.TotalReceived)
                : filtered.OrderBy(x => x.TotalReceived),
            "LastReceivedAt" => request.SortDescending
                ? filtered.OrderByDescending(x => x.LastReceivedAt)
                : filtered.OrderBy(x => x.LastReceivedAt),
            _ => request.SortDescending
                ? filtered.OrderByDescending(x => x.CreatedAt)
                : filtered.OrderBy(x => x.CreatedAt),
        };
    }
}
