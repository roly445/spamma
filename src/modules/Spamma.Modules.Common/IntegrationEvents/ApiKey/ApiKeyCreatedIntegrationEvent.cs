using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.ApiKey;

public record ApiKeyCreatedIntegrationEvent(
    Guid ApiKeyId,
    Guid UserId,
    string Name,
    DateTime WhenHappened) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ApiKeyCreated;
}