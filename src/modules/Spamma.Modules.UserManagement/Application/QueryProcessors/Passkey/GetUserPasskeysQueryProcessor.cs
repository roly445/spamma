using BluQube.Queries;
using Marten;
using Microsoft.Extensions.Logging;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors;

internal class GetUserPasskeysQueryProcessor(
    IDocumentSession documentSession,
    ILogger<GetUserPasskeysQueryProcessor> logger) : IQueryProcessor<GetUserPasskeysQuery, GetUserPasskeysQueryResult>
{
    public async Task<QueryResult<GetUserPasskeysQueryResult>> Handle(GetUserPasskeysQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // Query the read model projection for optimal performance
            var readModels = await documentSession
                .Query<PasskeyLookup>()
                .Where(p => p.UserId == query.UserId)
                .OrderByDescending(p => p.RegisteredAt)
                .ToListAsync(cancellationToken);

            var summaries = readModels.Select(p => new PasskeySummary(
                p.Id,
                p.DisplayName,
                p.Algorithm,
                p.RegisteredAt,
                p.LastUsedAt,
                p.IsRevoked,
                p.RevokedAt)).ToList();

            return QueryResult<GetUserPasskeysQueryResult>.Succeeded(new GetUserPasskeysQueryResult(summaries));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user passkeys for user {UserId}", query.UserId);
            return QueryResult<GetUserPasskeysQueryResult>.Failed();
        }
    }
}