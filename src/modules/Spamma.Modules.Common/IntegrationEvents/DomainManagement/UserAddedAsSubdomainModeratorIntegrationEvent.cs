using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record UserAddedAsSubdomainModeratorIntegrationEvent(
    Guid UserId,
    Guid SubdomainId,
    string UserName,
    string UserEmail) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserAddedAsSubdomainModerator;
}