using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.EmailInbox;

public record EmailDeletedIntegrationEvent(Guid EmailId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.EmailDeleted;
}