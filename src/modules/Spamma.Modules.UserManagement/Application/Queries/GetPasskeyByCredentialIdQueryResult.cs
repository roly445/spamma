using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Application.Queries;

internal record GetPasskeyByCredentialIdQueryResult(
    Guid PasskeyId,
    Guid UserId,
    byte[] PublicKey,
    uint SignCount,
    DateTime RegisteredAt,
    DateTime? LastUsedAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    Guid? RevokedByUserId) : IQueryResult;
