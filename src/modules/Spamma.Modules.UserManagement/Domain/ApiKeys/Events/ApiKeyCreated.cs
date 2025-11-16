namespace Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

public record ApiKeyCreated(
    Guid ApiKeyId,
    Guid UserId,
    string Name,
    string KeyHashPrefix,
    string KeyHash,
    DateTime CreatedAt,
    DateTime ExpiresAt);