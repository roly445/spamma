using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.Common.IntegrationEvents.EmailInbox;

/// <summary>
/// Represents a received email that has been accepted by SMTP and stored to disk.
/// Published to CAP for asynchronous processing (database writes, campaign recording, chaos address handling).
/// </summary>
public record EmailReceivedIntegrationEvent(
    Guid EmailId,
    Guid DomainId,
    Guid SubdomainId,
    string? Subject,
    DateTime ReceivedAt,
    List<EmailReceivedAddress> Recipients,
    string? CampaignValue,
    string FromAddress,
    string ToAddress,
    Guid? ChaosAddressId) : IIntegrationEvent
{
    public string EventName => IntegrationEventNames.EmailReceived;
}