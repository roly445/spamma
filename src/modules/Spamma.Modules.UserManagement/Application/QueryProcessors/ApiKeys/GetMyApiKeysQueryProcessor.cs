using System.Security.Claims;
using BluQube;
using BluQube.Commands;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Application.DTOs;
using Spamma.Modules.UserManagement.Client.Application.Queries.ApiKeys;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.ApiKeys;

public class GetMyApiKeysQueryProcessor(IDocumentSession session, IHttpContextAccessor httpContextAccessor)
    : IQueryProcessor<GetMyApiKeysQuery, GetMyApiKeysQueryResult>
{
    public async Task<QueryResult<GetMyApiKeysQueryResult>> Handle(GetMyApiKeysQuery request, CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return QueryResult<GetMyApiKeysQueryResult>.Failed();
        }

        var apiKeys = await session.Query<ApiKeyLookup>()
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var apiKeySummaries = apiKeys.Select(apiKey => new ApiKeySummary(
            apiKey.Id,
            apiKey.Name,
            apiKey.CreatedAt.UtcDateTime,
            apiKey.IsRevoked,
            apiKey.RevokedAt.HasValue ? apiKey.RevokedAt.Value.UtcDateTime : (DateTime?)null,
            apiKey.ExpiresAt ?? DateTimeOffset.MaxValue));

        return QueryResult<GetMyApiKeysQueryResult>.Succeeded(new GetMyApiKeysQueryResult(apiKeySummaries));
    }
}