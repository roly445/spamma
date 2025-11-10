namespace Spamma.Modules.EmailInbox.Domain.PushIntegrationAggregate.Events;

/// <summary>
/// Event raised when a push integration is deleted.
/// </summary>
public record PushIntegrationDeletedEvent(DateTimeOffset DeletedAt);