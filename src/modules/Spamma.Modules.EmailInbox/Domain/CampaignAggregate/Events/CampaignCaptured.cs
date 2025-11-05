namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignCaptured(
    Guid CampaignId,
    Guid MessageId,
    DateTimeOffset CapturedAt);
