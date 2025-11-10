using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

public record ApiKeyCreated(
    Guid ApiKeyId,
    Guid UserId,
    string Name,
    string KeyHash,
    string Salt,
    DateTimeOffset CreatedAt);