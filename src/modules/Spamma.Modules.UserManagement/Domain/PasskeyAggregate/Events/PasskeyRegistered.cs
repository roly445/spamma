namespace Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Events;

public record PasskeyRegistered(
    Guid PasskeyId,
    Guid UserId,
    byte[] CredentialId,
    byte[] PublicKey,
    uint SignCount,
    string DisplayName,
    string Algorithm,
    DateTime RegisteredAt);