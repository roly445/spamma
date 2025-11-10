using Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate;

namespace Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.Events;

/// <summary>
/// Event raised when a push integration is updated.
/// </summary>
public record PushIntegrationUpdatedEvent(
    string? Name,
    string EndpointUrl,
    FilterType FilterType,
    string? FilterValue,
    DateTimeOffset UpdatedAt);