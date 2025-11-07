namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignCaptured(
    Guid MessageId,
    DateTimeOffset CapturedAt);
