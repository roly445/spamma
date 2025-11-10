using FluentAssertions;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

public class CampaignBuilderTests
{
    [Fact]
    public void Build_WithDefaultValues_CreatesValidCampaign()
    {
        var campaign = new CampaignBuilder().Build();

        campaign.Should().NotBeNull();
        campaign.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Build_WithCustomCampaignId_SetsId()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new CampaignBuilder()
            .WithCampaignId(campaignId)
            .Build();

        campaign.Id.Should().Be(campaignId);
    }

    [Fact]
    public void Build_FluentChaining_Succeeds()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var campaign = new CampaignBuilder()
            .WithDomainId(domainId)
            .WithSubdomainId(subdomainId)
            .WithCampaignValue("test-value")
            .Build();

        campaign.Should().NotBeNull();
    }
}
