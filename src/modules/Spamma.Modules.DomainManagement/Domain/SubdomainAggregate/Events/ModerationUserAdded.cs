namespace Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events;

public record ModerationUserAdded(Guid UserId, DateTime WhenAdded);