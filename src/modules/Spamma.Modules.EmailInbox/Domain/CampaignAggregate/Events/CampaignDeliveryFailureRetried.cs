namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignDeliveryFailureRetried(Guid FailureId, Guid AttemptId, DateTimeOffset ObservedAt);
