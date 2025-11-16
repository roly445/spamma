namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AuthenticationCompleted(Guid AuthenticationAttemptId, DateTime CompletedAt, Guid SecurityStamp);