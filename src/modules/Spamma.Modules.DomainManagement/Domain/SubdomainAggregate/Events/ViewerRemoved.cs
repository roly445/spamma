namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record ViewerRemoved(Guid UserId, DateTime WhenRemoved);