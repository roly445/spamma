using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.UserManagement;

public record UserSuspendedIntegrationEvent(
    Guid UserId, DateTime WhenHappened) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserSuspended;
}