using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

public record ApiKeyRevoked(
    Guid ApiKeyId,
    DateTimeOffset RevokedAt);