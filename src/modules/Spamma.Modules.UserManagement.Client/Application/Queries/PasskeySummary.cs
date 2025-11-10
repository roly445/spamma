namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record PasskeySummary(
    Guid Id,
    string DisplayName,
    string Algorithm,
    DateTime RegisteredAt,
    DateTime? LastUsedAt,
    bool IsRevoked,
    DateTime? RevokedAt);