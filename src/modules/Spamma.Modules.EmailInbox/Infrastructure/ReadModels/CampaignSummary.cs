namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class CampaignSummary
{
    public Guid CampaignId { get; internal set; }

    public Guid DomainId { get; internal set; }

    public Guid SubdomainId { get; internal set; }

    public string CampaignValue { get; internal set; } = string.Empty;

    public Guid? SampleMessageId { get; internal set; }

    public DateTimeOffset FirstReceivedAt { get; internal set; }

    public DateTimeOffset LastReceivedAt { get; internal set; }

    public int TotalCaptured { get; internal set; }
}