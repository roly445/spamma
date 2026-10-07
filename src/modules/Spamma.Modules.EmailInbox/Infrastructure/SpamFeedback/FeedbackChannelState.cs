namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public sealed record FeedbackChannelState(
    FeedbackDeliveryStatus Status,
    int Attempts = 0,
    string? LastError = null,
    DateTimeOffset? DeliveredAt = null,
    DateTimeOffset? NextAttemptAt = null);
