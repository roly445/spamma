using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

public class SearchEmailsQueryProcessor(IDocumentSession documentSession, IHttpContextAccessor accessor) : IQueryProcessor<SearchEmailsQuery, SearchEmailsQueryResult>
{
    public async Task<QueryResult<SearchEmailsQueryResult>> Handle(SearchEmailsQuery request, CancellationToken cancellationToken)
    {
        var domainClaims = accessor.HttpContext?.User.FindAll(Lookups.ViewableSubdomainClaim).Select(x =>
        {
            if (Guid.TryParse(x.Value, out var d))
            {
                return d;
            }

            return (Guid?)null;
        }).Where(x => x.HasValue).Select(x => x!.Value).ToList();

        var query = documentSession.Query<EmailLookup>()
            .Where(x => domainClaims != null && domainClaims.Contains(x.SubdomainId) && x.WhenDeleted == null);

        // Apply search filtering if search text is provided
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim().ToLowerInvariant();

            query = query.Where(x =>
                x.Subject.ToLowerInvariant().Contains(searchText) ||
                x.EmailAddresses.Any(ea => ea.Address.ToLowerInvariant().Contains(searchText) ||
                                          ea.Name.ToLowerInvariant().Contains(searchText)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Calculate pagination values
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize)); // Limit max page size to 100
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var skip = (page - 1) * pageSize;

        // Apply pagination and get results
        var emails = await query
            .OrderByDescending(e => e.WhenSent)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(token: cancellationToken);

        var result = new SearchEmailsQueryResult(
            Items: emails.Select(x => new SearchEmailsQueryResult.EmailSummary(x.Id, x.Subject, x.EmailAddresses.First(y => y.EmailAddressType == EmailAddressType.To).Address, x.WhenSent, x.IsFavorite)).ToList(),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages);

        return QueryResult<SearchEmailsQueryResult>.Succeeded(result);
    }
}