namespace Spamma.Modules.UserManagement.Client.Application.DTOs;

public record ApiKeySummary(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    bool IsRevoked,
    DateTime? RevokedAt);