namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class CampaignSample
{
    public Guid CampaignId { get; set; }

    public Guid MessageId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset StoredAt { get; set; }

    public string ContentPreview { get; set; } = string.Empty;
}