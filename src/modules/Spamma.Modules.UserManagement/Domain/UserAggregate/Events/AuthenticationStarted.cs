namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AuthenticationStarted(Guid AuthenticationAttemptId, DateTime WhenStarted);