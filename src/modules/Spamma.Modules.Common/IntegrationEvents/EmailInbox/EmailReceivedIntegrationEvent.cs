using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.EmailInbox;

public record EmailReceivedIntegrationEvent(
    Guid EmailId,
    Guid DomainId,
    Guid SubdomainId,
    string? Subject,
    DateTime ReceivedAt,
    List<EmailReceivedIntegrationEvent.EmailReceivedAddress> Recipients,
    string? CampaignValue,
    string FromAddress,
    string ToAddress,
    Guid? ChaosAddressId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.EmailReceived;

    public sealed record EmailReceivedAddress(string Address, string? DisplayName, int Type);
}