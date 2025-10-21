namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record SubdomainCreated(Guid SubdomainId, Guid DomainId, string Name, DateTime WhenCreated, string? Description);