namespace Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

public record ModerationUserRemoved(Guid UserId, DateTime RemovedAt);