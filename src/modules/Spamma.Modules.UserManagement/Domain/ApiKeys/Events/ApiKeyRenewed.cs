using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Domain.ApiKeys.Events;

public record ApiKeyRenewed(
    Guid ApiKeyId,
    string NewKeyHash,
    string NewSalt,
    DateTimeOffset RenewedAt,
    DateTimeOffset NewExpiresAt);