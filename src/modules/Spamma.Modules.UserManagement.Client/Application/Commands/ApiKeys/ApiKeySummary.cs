namespace Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

public record ApiKeySummary(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    bool IsRevoked,
    DateTime? RevokedAt);