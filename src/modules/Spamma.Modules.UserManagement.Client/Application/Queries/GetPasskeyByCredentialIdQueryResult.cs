using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record GetPasskeyByCredentialIdQueryResult(
    Guid PasskeyId,
    Guid UserId,
    byte[] PublicKey,
    uint SignCount,
    DateTime RegisteredAt,
    DateTime? LastUsedAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    Guid? RevokedByUserId) : IQueryResult;