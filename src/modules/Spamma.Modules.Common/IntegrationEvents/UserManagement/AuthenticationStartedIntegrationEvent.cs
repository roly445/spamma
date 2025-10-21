using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.UserManagement;

public record AuthenticationStartedIntegrationEvent(
    Guid UserId, Guid SecurityStamp, Guid AuthenticationAttemptId, string Name, string EmailAddress, DateTime WhenHappened) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.AuthenticationStarted;
}