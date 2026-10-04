namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignCaptured(DateTimeOffset CapturedAt, Guid MessageId = default);
