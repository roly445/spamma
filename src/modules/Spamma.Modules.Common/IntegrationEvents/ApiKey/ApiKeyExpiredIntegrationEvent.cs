using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

/// <summary>
/// Integration event raised when an API key expires.
/// </summary>
public record ApiKeyExpiredIntegrationEvent(
    Guid ApiKeyId,
    DateTimeOffset ExpiredAt) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyExpired;
}