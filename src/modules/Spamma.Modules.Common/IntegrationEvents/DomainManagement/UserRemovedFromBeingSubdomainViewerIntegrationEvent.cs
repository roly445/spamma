using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.DomainManagement;

public record UserRemovedFromBeingSubdomainViewerIntegrationEvent(Guid UserId, Guid SubdomainId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.UserRemovedFromBeingSubdomainViewer;
}