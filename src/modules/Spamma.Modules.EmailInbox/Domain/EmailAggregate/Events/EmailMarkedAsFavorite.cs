namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

/// <summary>
/// Event raised when an email is marked as a favorite to prevent automatic deletion.
/// </summary>
public record EmailMarkedAsFavorite(DateTime WhenMarked);