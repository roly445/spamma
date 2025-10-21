namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

public record ModerationUserAdded(Guid UserId, DateTime WhenAdded);