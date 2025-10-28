namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

/// <summary>
/// Raised when a passkey is revoked
/// </summary>
public record PasskeyRevoked(
    Guid RevokedByUserId,
    DateTime RevokedAt);