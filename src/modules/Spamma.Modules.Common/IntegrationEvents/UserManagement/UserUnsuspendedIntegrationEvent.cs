using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.UserManagement;

public record UserUnsuspendedIntegrationEvent(
    Guid UserId, DateTime WhenHappened) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserUnsuspended;
}