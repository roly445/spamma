namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

#pragma warning disable S1144 // Private setters are used by Marten's Patch API via reflection

public class CampaignSummary
{
    public Guid CampaignId { get; init; }

    public Guid DomainId { get; init; }

    public Guid SubdomainId { get; init; }

    public string CampaignValue { get; init; } = string.Empty;

    public Guid? SampleMessageId { get; init; }

    public DateTimeOffset FirstReceivedAt { get; init; }

    public DateTimeOffset LastReceivedAt { get; init; }

    public int TotalCaptured { get; init; }
}