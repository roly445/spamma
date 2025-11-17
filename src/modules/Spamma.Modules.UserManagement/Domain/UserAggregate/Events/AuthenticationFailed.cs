namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AuthenticationFailed(Guid AuthenticationAttemptId, DateTime FailedAt, Guid SecurityStamp);