using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AuthenticationStarted(Guid AuthenticationAttemptId, DateTime WhenStarted);