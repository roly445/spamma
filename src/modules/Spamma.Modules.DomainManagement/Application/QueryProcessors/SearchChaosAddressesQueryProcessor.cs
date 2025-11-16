using System.Linq.Expressions;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class SearchChaosAddressesQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor) : IQueryProcessor<SearchChaosAddressesQuery, SearchChaosAddressesQueryResult>
{
    public async Task<QueryResult<SearchChaosAddressesQueryResult>> Handle(SearchChaosAddressesQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<ChaosAddressLookup>();

        var filteredAndSorted = this.GetFilteredAndSortedQuery(baseQuery, request);

        var totalCount = await filteredAndSorted.CountAsync(cancellationToken);

        var items = await filteredAndSorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var summaries = items.Select(x => new ChaosAddressSummary(
            x.Id,
            x.DomainId,
            x.SubdomainId,
            x.LocalPart,
            x.ConfiguredSmtpCode,
            x.Enabled,
            x.TotalReceived,
            x.LastReceivedAt,
            x.CreatedAt)).ToList();

        var result = new SearchChaosAddressesQueryResult(summaries, totalCount, request.PageNumber, request.PageSize);
        return QueryResult<SearchChaosAddressesQueryResult>.Succeeded(result);
    }

    private IQueryable<ChaosAddressLookup> GetFilteredAndSortedQuery(IQueryable<ChaosAddressLookup> query, SearchChaosAddressesQuery request)
    {
        var whereConditions = new List<Expression<Func<ChaosAddressLookup, bool>>>();

        var skipDomains = false;
        var user = accessor.HttpContext.ToUserAuthInfo();
        if (user.SystemRole.HasFlag(SystemRole.DomainManagement))
        {
            skipDomains = true;
        }

        if (!skipDomains)
        {
            whereConditions.Add(u =>
                user.ModeratedDomains.Contains(u.DomainId) ||
                user.ModeratedSubdomains.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            whereConditions.Add(x => x.LocalPart.ToLower().Contains(searchTerm));
        }

        if (request.SubdomainId.HasValue)
        {
            whereConditions.Add(x => x.SubdomainId == request.SubdomainId.Value);
        }

        if (request.Enabled.HasValue)
        {
            whereConditions.Add(x => x.Enabled == request.Enabled.Value);
        }

        var filtered = whereConditions.Aggregate(query, (current, condition) => current.Where(condition));

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