using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record UserRemovedFromBeingDomainModeratorIntegrationEvent(Guid UserId, Guid DomainId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserRemovedFromBeingDomainModerator;
}