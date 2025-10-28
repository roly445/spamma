namespace Spamma.Modules.UserManagement.Client.Application.Queries;

/// <summary>
/// Summary information about a passkey.
/// </summary>
public record PasskeySummary(
    Guid Id,
    string DisplayName,
    string Algorithm,
    DateTime RegisteredAt,
    DateTime? LastUsedAt,
    bool IsRevoked,
    DateTime? RevokedAt);