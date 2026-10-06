namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record SpamFeedbackSettingsRequest(string? ArfRecipient, string? WebhookUrl);
