namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record ModerationUserRemoved(Guid UserId, DateTime WhenRemoved);