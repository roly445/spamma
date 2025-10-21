using System.Linq.Expressions;
using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

public class SearchSubdomainViewersQueryProcessor(IDocumentSession session) : IQueryProcessor<SearchSubdomainViewersQuery, SearchSubdomainViewersQueryResult>
{
    public async Task<QueryResult<SearchSubdomainViewersQueryResult>> Handle(SearchSubdomainViewersQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<SubdomainLookup>()
            .Where(d => d.Id == request.SubdomainId)
            .SelectMany(d => d.Viewers)
            .AsQueryable();

        var whereConditions = new List<Expression<Func<Viewer, bool>>>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            whereConditions.Add(m =>
                m.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        IQueryable<Viewer> filteredQuery = baseQuery;
        foreach (var condition in whereConditions)
        {
            filteredQuery = filteredQuery.Where(condition);
        }

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