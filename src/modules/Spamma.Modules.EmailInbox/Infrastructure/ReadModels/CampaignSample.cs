namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class CampaignSample
{
    public Guid CampaignId { get; init; }

    public Guid MessageId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; init; }

    public DateTimeOffset StoredAt { get; init; }

    public string ContentPreview { get; init; } = string.Empty;
}