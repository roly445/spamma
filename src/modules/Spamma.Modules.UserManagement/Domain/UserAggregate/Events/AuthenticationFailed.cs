namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AuthenticationFailed(Guid AuthenticationAttemptId, DateTime WhenFailed, Guid SecurityStamp);