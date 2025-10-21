namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record ViewerAdded(Guid UserId, DateTime WhenAdded);