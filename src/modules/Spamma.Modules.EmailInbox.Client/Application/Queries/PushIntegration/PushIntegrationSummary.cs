using Spamma.Modules.EmailInbox.Client.Application;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries.PushIntegration;

/// <summary>
/// Summary of a push integration.
/// </summary>
public record PushIntegrationSummary(
    Guid Id,
    Guid SubdomainId,
    string? Name,
    string EndpointUrl,
    FilterType FilterType,
    string? FilterValue,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);