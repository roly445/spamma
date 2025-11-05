using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record SubdomainStatusChangedIntegrationEvent(Guid SubdomainId, string DomainName) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.SubdomainStatusChanged;
}
