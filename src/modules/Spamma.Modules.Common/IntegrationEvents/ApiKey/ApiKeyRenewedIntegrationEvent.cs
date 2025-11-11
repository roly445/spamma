using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

/// <summary>
/// Integration event raised when an API key is renewed.
/// </summary>
public record ApiKeyRenewedIntegrationEvent(
    Guid ApiKeyId,
    DateTimeOffset RenewedAt,
    DateTimeOffset NewExpiresAt) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyRenewed;
}