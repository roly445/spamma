using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries.PushIntegration;

/// <summary>
/// Result of getting push integrations.
/// </summary>
public record GetPushIntegrationsQueryResult(IReadOnlyList<PushIntegrationSummary> Integrations) : IQueryResult;