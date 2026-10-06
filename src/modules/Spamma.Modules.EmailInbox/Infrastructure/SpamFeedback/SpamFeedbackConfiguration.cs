namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record SpamFeedbackConfiguration(
    Guid Id,
    string? ArfRecipient,
    string? WebhookUrl,
    string? ProtectedWebhookSecret);
