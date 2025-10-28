using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Detailed information about a passkey.
/// </summary>
public record PasskeyDetailsResult(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Algorithm,
    DateTime RegisteredAt,
    DateTime? LastUsedAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    Guid? RevokedByUserId) : IQueryResult;