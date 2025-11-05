namespace Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;

public record CampaignDeleted(
    Guid CampaignId,
    DateTimeOffset DeletedAt,
    bool Force);
