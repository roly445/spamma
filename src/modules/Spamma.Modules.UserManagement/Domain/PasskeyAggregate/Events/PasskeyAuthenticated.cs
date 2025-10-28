namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

/// <summary>
/// Raised when a passkey is used for successful authentication
/// </summary>
public record PasskeyAuthenticated(
    uint NewSignCount,
    DateTime UsedAt);