using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.UserManagement;

public record UserDetailsUpdatedIntegrationEvent(Guid UserId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserDetailsUpdated;
}