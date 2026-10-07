namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record SpamReport(
    Guid Id,
    Guid DomainId,
    Guid SubdomainId,
    Guid? CampaignId,
    string? CampaignValue,
    string Recipient,
    string Sender,
    SpamReportTrigger Trigger,
    DateTimeOffset CreatedAt,
    string? ArfRecipient,
    string? WebhookUrl,
    string? ProtectedWebhookSecret,
    FeedbackChannelState EmailDelivery,
    FeedbackChannelState WebhookDelivery,
    DateTimeOffset? NextAttemptAt);
