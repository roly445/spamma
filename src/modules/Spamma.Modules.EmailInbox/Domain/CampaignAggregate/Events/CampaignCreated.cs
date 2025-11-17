namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignCreated(
    Guid CampaignId,
    Guid DomainId,
    Guid SubdomainId,
    string CampaignValue,
    Guid MessageId,
    DateTime CreatedAt,
    DateTimeOffset ReceivedAt);