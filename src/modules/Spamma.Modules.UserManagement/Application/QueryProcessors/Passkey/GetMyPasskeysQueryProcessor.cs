using System.Security.Claims;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                logger.LogWarning("User ID claim not found in HTTP context");
                return QueryResult<GetMyPasskeysQueryResult>.Failed();
            }

            if (!Guid.TryParse(userIdClaim, out var currentUserId))
            {
                logger.LogWarning("Unable to parse user ID from claims: {UserIdClaim}", userIdClaim);
                return QueryResult<GetMyPasskeysQueryResult>.Failed();
            }

            logger.LogInformation("Retrieving passkeys for user: {UserId}", currentUserId);

            var readModels = await documentSession
                .Query<PasskeyLookup>()
                .Where(p => p.UserId == currentUserId)
                .OrderByDescending(p => p.RegisteredAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Found {PasskeyCount} passkeys for user {UserId}", readModels.Count, currentUserId);

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