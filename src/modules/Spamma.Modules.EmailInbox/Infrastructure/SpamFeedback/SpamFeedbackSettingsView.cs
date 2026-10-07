namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record SpamFeedbackSettingsView(
    string? ArfRecipient,
    string? WebhookUrl,
    bool HasWebhookSecret,
    string? NewWebhookSecret = null);
