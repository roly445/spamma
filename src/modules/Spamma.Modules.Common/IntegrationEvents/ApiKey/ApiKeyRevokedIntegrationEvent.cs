using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

public record ApiKeyRevokedIntegrationEvent(
    Guid ApiKeyId,
    Guid UserId,
    DateTime RevokedAt) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyRevoked;
}