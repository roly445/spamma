using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

public record ApiKeyExpiredIntegrationEvent(
    Guid ApiKeyId,
    DateTimeOffset ExpiredAt) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyExpired;
}