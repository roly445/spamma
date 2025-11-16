using System.Linq.Expressions;
using BluQube.Queries;
using Marten;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.QueryProcessors;

internal class SearchSubdomainModeratorsQueryProcessor(IDocumentSession session) : IQueryProcessor<SearchSubdomainModeratorsQuery, SearchSubdomainModeratorsQueryResult>
{
    public async Task<QueryResult<SearchSubdomainModeratorsQueryResult>> Handle(SearchSubdomainModeratorsQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = session.Query<SubdomainLookup>()
            .Where(d => d.Id == request.SubdomainId)
            .SelectMany(d => d.SubdomainModerators)
            .AsQueryable();

        var whereConditions = new List<Expression<Func<SubdomainModerator, bool>>>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            whereConditions.Add(m =>
                m.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var filteredQuery = whereConditions.Aggregate(baseQuery, (current, condition) => current.Where(condition));

        var orderedQuery = request.SortBy.ToLowerInvariant() switch
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

        var domainModeratorSummaries = items.Select(m => new SearchSubdomainModeratorsQueryResult.SubdomainModeratorSummary(
            m.UserId,
            m.Name,
            m.Email,
            m.CreatedAt)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return QueryResult<SearchSubdomainModeratorsQueryResult>.Succeeded(new SearchSubdomainModeratorsQueryResult(
            domainModeratorSummaries,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }
}