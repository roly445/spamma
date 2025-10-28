using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record UserAddedAsDomainModeratorIntegrationEvent(Guid UserId, Guid DomainId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserAddedAsDomainModerator;
}