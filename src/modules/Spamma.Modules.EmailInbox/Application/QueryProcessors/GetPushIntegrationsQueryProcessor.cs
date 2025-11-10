using System.Security.Claims;
using BluQube.Queries;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Application;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Client.Application;
using Spamma.Modules.EmailInbox.Client.Application.Queries.PushIntegration;
using Spamma.Modules.EmailInbox.Infrastructure.Repositories;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors;

/// <summary>
/// Processor for GetPushIntegrationsQuery.
/// </summary>
internal class GetPushIntegrationsQueryProcessor(
    IPushIntegrationQueryRepository pushIntegrationQueryRepository,
    IHttpContextAccessor httpContextAccessor)
    : IQueryProcessor<GetPushIntegrationsQuery, GetPushIntegrationsQueryResult>
{
    /// <inheritdoc />
    public async Task<QueryResult<GetPushIntegrationsQueryResult>> Handle(
        GetPushIntegrationsQuery query,
        CancellationToken cancellationToken)
    {
        // Get current authenticated user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return QueryResult<GetPushIntegrationsQueryResult>.Failed();
        }

        var integrations = await pushIntegrationQueryRepository.GetByUserIdAsync(userId);
        var items = integrations.Select(i => new PushIntegrationSummary(
            i.Id,
            i.SubdomainId,
            i.Name ?? string.Empty,
            i.EndpointUrl,
            (Spamma.Modules.EmailInbox.Client.Application.FilterType)i.FilterType,
            i.FilterValue ?? string.Empty,
            i.IsActive,
            i.CreatedAt,
            i.UpdatedAt)).ToList();

        return QueryResult<GetPushIntegrationsQueryResult>.Succeeded(
            new GetPushIntegrationsQueryResult(items));
    }
}