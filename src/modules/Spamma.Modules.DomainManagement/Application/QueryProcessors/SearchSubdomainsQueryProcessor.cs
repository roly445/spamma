using System.Linq.Expressions;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class SearchSubdomainsQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor) : IQueryProcessor<SearchSubdomainsQuery, SearchSubdomainsQueryResult>
{
    public async Task<QueryResult<SearchSubdomainsQueryResult>> Handle(SearchSubdomainsQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<SubdomainLookup>();
        var whereConditions = new List<Expression<Func<SubdomainLookup, bool>>>();

        if (request.ParentDomainId.HasValue && request.ParentDomainId != Guid.Empty)
        {
            whereConditions.Add(x => x.DomainId == request.ParentDomainId.Value);
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                whereConditions.Add(x =>
                    x.SubdomainName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (x.Description != null && x.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                whereConditions.Add(x =>
                    x.SubdomainName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    x.FullName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (x.Description != null && x.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }
        }

        var skipDomains = false;
        var user = accessor.HttpContext.ToUserAuthInfo();
        if (user.IsAuthenticated)
        {
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
        }

        var filteredQuery = whereConditions.Aggregate<Expression<Func<SubdomainLookup, bool>>, IQueryable<SubdomainLookup>>(baseQuery, (current, condition) => current.Where(condition));

        var orderedQuery = request.SortBy.ToLowerInvariant() switch
         {
             "subdomainname" => request.SortDescending
                 ? filteredQuery.OrderByDescending(s => s.SubdomainName)
                 : filteredQuery.OrderBy(s => s.SubdomainName),
             "parentdomainname" => request.SortDescending
                 ? filteredQuery.OrderByDescending(s => s.ParentName)
                 : filteredQuery.OrderBy(s => s.ParentName),
             _ => request.SortDescending
                 ? filteredQuery.OrderByDescending(s => s.CreatedAt)
                 : filteredQuery.OrderBy(s => s.CreatedAt),
         };

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
             .Skip((request.Page - 1) * request.PageSize)
             .Take(request.PageSize)
             .ToListAsync(cancellationToken);

        var subdomainSummaries = items.Select(d => new SearchSubdomainsQueryResult.SubdomainSummary(
            d.Id,
            d.DomainId,
            d.SubdomainName,
            d.ParentName,
            d.FullName,
            GetSubdomainStatus(d),
            d.CreatedAt,
            d.ChaosMonkeyRuleCount,
            d.ActiveCampaignCount,
            d.Description)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return QueryResult<SearchSubdomainsQueryResult>.Succeeded(new SearchSubdomainsQueryResult(
             subdomainSummaries,
             totalCount,
             request.Page,
             request.PageSize,
             totalPages));
    }

    private static SubdomainStatus GetSubdomainStatus(SubdomainLookup domain)
    {
        return domain.IsSuspended ? SubdomainStatus.Suspended : SubdomainStatus.Active;
    }
}