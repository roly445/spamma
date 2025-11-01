namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

/// <summary>
/// Event raised when an email is unmarked as a favorite and becomes eligible for automatic deletion.
/// </summary>
public record EmailUnmarkedAsFavorite(DateTime WhenUnmarked);