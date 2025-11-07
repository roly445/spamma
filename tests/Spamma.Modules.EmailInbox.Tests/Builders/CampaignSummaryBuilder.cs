using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

/// <summary>
/// Fluent builder for CampaignSummary read model to support test data setup.
/// </summary>
public class CampaignSummaryBuilder
{
    private Guid _campaignId = Guid.NewGuid();
    private Guid _domainId = Guid.NewGuid();
    private Guid _subdomainId = Guid.NewGuid();
    private string _campaignValue = "test@example.com";
    private Guid? _sampleMessageId = Guid.NewGuid();
    private DateTimeOffset _firstReceivedAt = DateTimeOffset.UtcNow.AddHours(-1);
    private DateTimeOffset _lastReceivedAt = DateTimeOffset.UtcNow;
    private int _totalCaptured = 1;

    public CampaignSummaryBuilder WithCampaignId(Guid campaignId)
    {
        this._campaignId = campaignId;
        return this;
    }

    public CampaignSummaryBuilder WithDomainId(Guid domainId)
    {
        this._domainId = domainId;
        return this;
    }

    public CampaignSummaryBuilder WithSubdomainId(Guid subdomainId)
    {
        this._subdomainId = subdomainId;
        return this;
    }

    public CampaignSummaryBuilder WithCampaignValue(string campaignValue)
    {
        this._campaignValue = campaignValue;
        return this;
    }

    public CampaignSummaryBuilder WithSampleMessageId(Guid? sampleMessageId)
    {
        this._sampleMessageId = sampleMessageId;
        return this;
    }

    public CampaignSummaryBuilder WithFirstReceivedAt(DateTimeOffset firstReceivedAt)
    {
        this._firstReceivedAt = firstReceivedAt;
        return this;
    }

    public CampaignSummaryBuilder WithLastReceivedAt(DateTimeOffset lastReceivedAt)
    {
        this._lastReceivedAt = lastReceivedAt;
        return this;
    }

    public CampaignSummaryBuilder WithTotalCaptured(int totalCaptured)
    {
        this._totalCaptured = totalCaptured;
        return this;
    }

    public CampaignSummary Build()
    {
        return new CampaignSummary
        {
            CampaignId = this._campaignId,
            DomainId = this._domainId,
            SubdomainId = this._subdomainId,
            CampaignValue = this._campaignValue,
            SampleMessageId = this._sampleMessageId,
            FirstReceivedAt = this._firstReceivedAt,
            LastReceivedAt = this._lastReceivedAt,
            TotalCaptured = this._totalCaptured,
        };
    }
}
