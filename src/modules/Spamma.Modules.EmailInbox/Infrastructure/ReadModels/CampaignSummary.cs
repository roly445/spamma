namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class CampaignSummary
{
    public Guid CampaignId { get; set; }

    public Guid DomainId { get; set; }

    public Guid SubdomainId { get; set; }

    public string CampaignValue { get; set; } = string.Empty;

    public Guid? SampleMessageId { get; set; }

    public DateTimeOffset FirstReceivedAt { get; set; }

    public DateTimeOffset LastReceivedAt { get; set; }

    public int TotalCaptured { get; set; }
}
