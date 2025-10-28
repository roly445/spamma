namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

/// <summary>
/// Raised when a new passkey is registered for a user
/// </summary>
public record PasskeyRegistered(
    Guid PasskeyId,
    Guid UserId,
    byte[] CredentialId,
    byte[] PublicKey,
    uint SignCount,
    string DisplayName,
    string Algorithm,
    DateTime RegisteredAt);