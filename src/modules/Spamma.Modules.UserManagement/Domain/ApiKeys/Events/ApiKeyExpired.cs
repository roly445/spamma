using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

public record ApiKeyExpired(
    Guid ApiKeyId,
    DateTimeOffset ExpiredAt);