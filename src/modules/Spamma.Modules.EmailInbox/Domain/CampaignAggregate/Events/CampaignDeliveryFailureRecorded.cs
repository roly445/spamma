namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignDeliveryFailureRecorded(Guid FailureId, Guid AttemptId, int SmtpCode, DateTimeOffset ObservedAt);
