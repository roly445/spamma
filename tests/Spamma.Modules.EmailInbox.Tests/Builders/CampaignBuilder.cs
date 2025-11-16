using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

public class CampaignBuilder
{
    private Guid _campaignId = Guid.NewGuid();
    private Guid _domainId = Guid.NewGuid();
    private Guid _subdomainId = Guid.NewGuid();
    private string _campaignValue = "test@example.com";
    private Guid _messageId = Guid.NewGuid();
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

    public CampaignBuilder WithCampaignId(Guid campaignId)
    {
        this._campaignId = campaignId;
        return this;
    }

    public CampaignBuilder WithDomainId(Guid domainId)
    {
        this._domainId = domainId;
        return this;
    }

    public CampaignBuilder WithSubdomainId(Guid subdomainId)
    {
        this._subdomainId = subdomainId;
        return this;
    }

    public CampaignBuilder WithCampaignValue(string campaignValue)
    {
        this._campaignValue = campaignValue;
        return this;
    }

    public CampaignBuilder WithMessageId(Guid messageId)
    {
        this._messageId = messageId;
        return this;
    }

    public CampaignBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        this._createdAt = createdAt;
        return this;
    }

    public Campaign Build()
    {
        var result = Campaign.Create(
            this._campaignId,
            this._domainId,
            this._subdomainId,
            this._campaignValue,
            this._messageId,
            this._createdAt);

        return result.Value;
    }
}