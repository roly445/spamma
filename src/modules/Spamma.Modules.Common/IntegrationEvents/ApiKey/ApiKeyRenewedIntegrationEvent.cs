using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

public record ApiKeyRenewedIntegrationEvent(
    Guid ApiKeyId,
    DateTimeOffset RenewedAt,
    DateTimeOffset NewExpiresAt) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyRenewed;
}