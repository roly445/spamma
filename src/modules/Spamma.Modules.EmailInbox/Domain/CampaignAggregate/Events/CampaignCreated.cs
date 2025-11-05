namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignCreated(
    Guid CampaignId,
    Guid SubdomainId,
    string CampaignValue,
    Guid MessageId,
    DateTimeOffset CreatedAt);
