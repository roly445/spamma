using System.Linq.Expressions;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class SearchSubdomainViewersQueryProcessor(IDocumentSession session, IHttpContextAccessor accessor) : IQueryProcessor<SearchSubdomainViewersQuery, SearchSubdomainViewersQueryResult>
{
    public async Task<QueryResult<SearchSubdomainViewersQueryResult>> Handle(SearchSubdomainViewersQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<SubdomainLookup>();
        var baseWhereConditions = new List<Expression<Func<SubdomainLookup, bool>>>
        {
            d => d.Id == request.SubdomainId,
        };

        var skipDomains = false;
        var user = accessor.HttpContext.ToUserAuthInfo();
        if (user.SystemRole.HasFlag(SystemRole.DomainManagement))
        {
            skipDomains = true;
        }

        if (!skipDomains)
        {
            baseWhereConditions.Add(u =>
                user.ModeratedDomains.Contains(u.DomainId) ||
                user.ModeratedSubdomains.Contains(u.Id));
        }

        IQueryable<SubdomainLookup> filteredBaseQuery = baseQuery;
        foreach (var condition in baseWhereConditions)
        {
            filteredBaseQuery = filteredBaseQuery.Where(condition);
        }

        var whereConditions = new List<Expression<Func<Viewer, bool>>>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            whereConditions.Add(m =>
                m.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var filteredQuery = filteredBaseQuery
            .SelectMany(d => d.Viewers)
            .AsQueryable();
        filteredQuery = whereConditions.Aggregate(filteredQuery, (current, condition) => current.Where(condition));

        IOrderedQueryable<Viewer> orderedQuery = request.SortBy.ToLowerInvariant() switch
        {
            "name" => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.Name)
                : filteredQuery.OrderBy(d => d.Name),
            "email" => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.Email)
                : filteredQuery.OrderBy(d => d.Email),
            "createdat" => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.CreatedAt)
                : filteredQuery.OrderBy(d => d.CreatedAt),
            _ => request.SortDescending
                ? filteredQuery.OrderByDescending(d => d.CreatedAt)
                : filteredQuery.OrderBy(d => d.CreatedAt),
        };

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var domainModeratorSummaries = items.Select(m => new SearchSubdomainViewersQueryResult.SubdomainViewerSummary(
            m.UserId,
            m.Name,
            m.Email,
            m.CreatedAt)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return QueryResult<SearchSubdomainViewersQueryResult>.Succeeded(new SearchSubdomainViewersQueryResult(
            domainModeratorSummaries,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }
}