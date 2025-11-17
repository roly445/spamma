using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record ChaosAddressUpdatedIntegrationEvent(Guid ChaosAddressId, Guid SubdomainId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.ChaosAddressUpdated;
}