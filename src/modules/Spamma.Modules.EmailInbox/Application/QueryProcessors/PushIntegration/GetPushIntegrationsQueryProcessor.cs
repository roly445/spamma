using System.Security.Claims;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.EmailInbox.Client.Application.Queries.PushIntegration;
using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.QueryProcessors.PushIntegration;

/// <summary>
/// Processor for getting push integrations.
/// </summary>
internal class GetPushIntegrationsQueryProcessor(
    IDocumentSession documentSession,
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

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return QueryResult<GetPushIntegrationsQueryResult>.Failed();
        }

        var pushIntegrations = await documentSession
            .Query<PushIntegrationLookup>()
            .Where(x => x.UserId == currentUserId && x.IsActive)
            .ToListAsync(cancellationToken);

        var summaries = pushIntegrations.Select(x => new PushIntegrationSummary(
            x.Id,
            x.SubdomainId,
            x.Name,
            x.EndpointUrl,
            (Client.Application.FilterType)x.FilterType,
            x.FilterValue,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt)).ToList();

        return QueryResult<GetPushIntegrationsQueryResult>.Succeeded(
            new GetPushIntegrationsQueryResult(summaries));
    }
}