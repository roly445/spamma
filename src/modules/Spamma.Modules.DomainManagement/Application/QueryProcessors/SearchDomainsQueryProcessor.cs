using System.Linq.Expressions;
using System.Security.Claims;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class SearchDomainsQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor) : IQueryProcessor<SearchDomainsQuery, SearchDomainsQueryResult>
{
    public async Task<QueryResult<SearchDomainsQueryResult>> Handle(SearchDomainsQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<DomainLookup>();

        var whereConditions = new List<Expression<Func<DomainLookup, bool>>>();
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            whereConditions.Add(d =>
                d.PrimaryContact != null && (d.DomainName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                             d.PrimaryContact.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply status filter
        if (request.Status.HasValue)
        {
            switch (request.Status.Value)
            {
                case DomainStatus.Pending:
                    whereConditions.Add(u => !u.IsVerified);
                    break;
                case DomainStatus.Active:
                    whereConditions.Add(u => u.IsVerified && !u.IsSuspended);
                    break;
                case DomainStatus.Suspended:
                    whereConditions.Add(u => u.IsSuspended);
                    break;
            }
        }

        var skipDomains = false;
        var claim = accessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
        if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement))
        {
            skipDomains = true;
        }

        if (!skipDomains)
        {
            var domainClaims = accessor.HttpContext?.User?.FindAll(Lookups.ModeratedDomainClaim).Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            var subdomainClaims = accessor.HttpContext?.User?.FindAll(Lookups.ModeratedSubdomainClaim).Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            whereConditions.Add(u => (domainClaims != null && domainClaims.Contains(u.Id)) || (subdomainClaims != null && u.Subdomains.Any(s => subdomainClaims.Contains(s.Id))));
        }

        // Apply verification filter
        if (request.IsVerified.HasValue)
        {
            whereConditions.Add(u => u.IsVerified);
        }

        // Apply all where conditions
        IQueryable<DomainLookup> filteredQuery = baseQuery;
        foreach (var condition in whereConditions)
        {
            filteredQuery = filteredQuery.Where(condition);
        }

        // Apply sorting
        IOrderedQueryable<DomainLookup> orderedQuery = request.SortBy.ToLowerInvariant() switch
        {
            "domainname" => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.DomainName)
                : filteredQuery.OrderBy(d => d.DomainName),
            "verifiedat" => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.VerifiedAt)
                : filteredQuery.OrderBy(d => d.VerifiedAt),
            "subdomaincount" => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.SubdomainCount)
                : filteredQuery.OrderBy(d => d.SubdomainCount),
            _ => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.CreatedAt)
                : filteredQuery.OrderBy(d => d.CreatedAt),
        };

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var domainSummaries = items.Select(d => new SearchDomainsQueryResult.DomainSummary(
            d.Id,
            d.DomainName,
            GetDomainStatus(d),
            d.IsVerified,
            d.CreatedAt,
            d.VerifiedAt,
            d.SubdomainCount, d.AssignedModeratorCount, d.PrimaryContact, d.Description, d.VerificationToken)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return QueryResult<SearchDomainsQueryResult>.Succeeded(new SearchDomainsQueryResult(
            domainSummaries,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }

    private static DomainStatus GetDomainStatus(DomainLookup domain)
    {
        if (domain.IsSuspended)
        {
            return DomainStatus.Suspended;
        }

        if (!domain.IsVerified)
        {
            return DomainStatus.Pending;
        }

        return DomainStatus.Active;
    }
}