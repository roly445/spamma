using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.EmailInbox;

public record EmailReceivedIntegrationEvent(Guid EmailId, Guid DomainId, Guid SubdomainId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.EmailReceived;
}