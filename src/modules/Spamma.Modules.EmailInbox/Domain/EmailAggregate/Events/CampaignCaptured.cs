namespace Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;

public record CampaignCaptured(
    Guid CampaignId,
    Guid SubdomainId,
    Guid MessageId,
    string CampaignValue,
    DateTimeOffset CapturedAt);
