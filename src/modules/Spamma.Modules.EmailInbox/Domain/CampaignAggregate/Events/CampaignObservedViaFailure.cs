namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignObservedViaFailure(Guid CampaignId, Guid DomainId, Guid SubdomainId, string CampaignValue, DateTimeOffset ObservedAt);
