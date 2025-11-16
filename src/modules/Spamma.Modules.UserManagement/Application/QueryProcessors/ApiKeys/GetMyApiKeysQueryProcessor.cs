using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.DTOs;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.ApiKeys;

internal class GetMyApiKeysQueryProcessor(IDocumentSession session, IHttpContextAccessor httpContextAccessor)
    : IQueryProcessor<GetMyApiKeysQuery, GetMyApiKeysQueryResult>
{
    public async Task<QueryResult<GetMyApiKeysQueryResult>> Handle(GetMyApiKeysQuery request, CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userAuthInfo = httpContextAccessor.HttpContext.ToUserAuthInfo();

        if (!userAuthInfo.IsAuthenticated)
        {
            return QueryResult<GetMyApiKeysQueryResult>.Failed();
        }

        var apiKeys = await session.Query<ApiKeyLookup>()
            .Where(x => x.UserId == userAuthInfo.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var apiKeySummaries = apiKeys.Select(apiKey => new ApiKeySummary(
            apiKey.Id,
            apiKey.Name,
            apiKey.CreatedAt.UtcDateTime,
            apiKey.IsRevoked,
            apiKey.RevokedAt?.UtcDateTime,
            apiKey.ExpiresAt ?? DateTimeOffset.MaxValue));

        return QueryResult<GetMyApiKeysQueryResult>.Succeeded(new GetMyApiKeysQueryResult(apiKeySummaries));
    }
}