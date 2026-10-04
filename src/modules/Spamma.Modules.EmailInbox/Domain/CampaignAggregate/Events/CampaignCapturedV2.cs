namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignCapturedV2(DateTimeOffset CapturedAt, Guid MessageId);
