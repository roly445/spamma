using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;

namespace Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.Events;

/// <summary>
/// Event raised when a push integration is created.
/// </summary>
public record PushIntegrationCreatedEvent(
    Guid PushIntegrationId,
    Guid UserId,
    Guid SubdomainId,
    string? Name,
    string EndpointUrl,
    FilterType FilterType,
    string? FilterValue,
    DateTimeOffset CreatedAt);