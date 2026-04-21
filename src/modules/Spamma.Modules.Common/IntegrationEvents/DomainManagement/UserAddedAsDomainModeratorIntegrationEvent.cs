using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record UserAddedAsDomainModeratorIntegrationEvent(
    Guid UserId,
    Guid DomainId,
    string UserName,
    string UserEmail) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserAddedAsDomainModerator;
}