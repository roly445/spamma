namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignFirstCaptured(Guid MessageId, DateTimeOffset CapturedAt);
