using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.Passkey;

internal class GetMyPasskeysQueryProcessor(
    IDocumentSession documentSession,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GetMyPasskeysQueryProcessor> logger) : IQueryProcessor<GetMyPasskeysQuery, GetMyPasskeysQueryResult>
{
    public async Task<QueryResult<GetMyPasskeysQueryResult>> Handle(GetMyPasskeysQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var userAuthInfo = httpContextAccessor.HttpContext.ToUserAuthInfo();

            if (!userAuthInfo.IsAuthenticated)
            {
                logger.LogWarning("User ID claim not found in HTTP context");
                return QueryResult<GetMyPasskeysQueryResult>.Failed();
            }

            logger.LogInformation("Retrieving passkeys for user: {UserId}", userAuthInfo.UserId);

            var readModels = await documentSession
                .Query<PasskeyLookup>()
                .Where(p => p.UserId == userAuthInfo.UserId)
                .OrderByDescending(p => p.RegisteredAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Found {PasskeyCount} passkeys for user {UserId}", readModels.Count, userAuthInfo.UserId);

            var summaries = readModels.Select(p => new PasskeySummary(
                p.Id,
                p.DisplayName,
                p.Algorithm,
                p.RegisteredAt,
                p.LastUsedAt,
                p.IsRevoked,
                p.RevokedAt)).ToList();

            return QueryResult<GetMyPasskeysQueryResult>.Succeeded(new GetMyPasskeysQueryResult(summaries));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving passkeys for current user");

            return QueryResult<GetMyPasskeysQueryResult>.Succeeded(new GetMyPasskeysQueryResult(new List<PasskeySummary>()));
        }
    }
}