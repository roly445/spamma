using System.Linq.Expressions;
using System.Security.Claims;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class SearchSubdomainsQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor) : IQueryProcessor<SearchSubdomainsQuery, SearchSubdomainsQueryResult>
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
        var claim = accessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
        if (claim == null || (Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement)))
        {
            skipDomains = true;
        }

        if (!skipDomains)
        {
            var domainClaims = accessor.HttpContext?.User?.FindAll("moderated_domain").Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            var subdomainClaims = accessor.HttpContext?.User?.FindAll("moderated_subdomain").Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            whereConditions.Add(u =>
                (domainClaims != null && domainClaims.Contains(u.DomainId)) ||
                (subdomainClaims != null && subdomainClaims.Contains(u.Id)));
        }

        IQueryable<SubdomainLookup> filteredQuery = baseQuery;
        foreach (var condition in whereConditions)
        {
            filteredQuery = filteredQuery.Where(condition);
        }

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
        if (domain.IsSuspended)
        {
            return SubdomainStatus.Suspended;
        }

        return SubdomainStatus.Active;
    }
}