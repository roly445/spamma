namespace Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;

public record ChaosAddressSubdomainChanged(Guid NewDomainId, Guid NewSubdomainId);
