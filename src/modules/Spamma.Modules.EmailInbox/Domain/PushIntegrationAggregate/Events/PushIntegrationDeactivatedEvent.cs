namespace Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.Events;

/// <summary>
/// Event raised when a push integration is deactivated.
/// </summary>
public record PushIntegrationDeactivatedEvent(DateTimeOffset DeactivatedAt);