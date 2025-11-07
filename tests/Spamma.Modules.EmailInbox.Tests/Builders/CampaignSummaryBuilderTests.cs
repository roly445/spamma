using FluentAssertions;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

public class CampaignSummaryBuilderTests
{
    [Fact]
    public void Build_WithDefaultValues_CreatesValidSummary()
    {
        var summary = new CampaignSummaryBuilder().Build();

        summary.Should().NotBeNull();
        summary.CampaignId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Build_WithCustomCampaignId_SetsCampaignId()
    {
        var campaignId = Guid.NewGuid();
        var summary = new CampaignSummaryBuilder()
            .WithCampaignId(campaignId)
            .Build();

        summary.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public void Build_WithCustomCampaignValue_SetsCampaignValue()
    {
        var campaignValue = "test-campaign";
        var summary = new CampaignSummaryBuilder()
            .WithCampaignValue(campaignValue)
            .Build();

        summary.CampaignValue.Should().Be(campaignValue);
    }

    [Fact]
    public void Build_FluentChaining_Succeeds()
    {
        var campaignId = Guid.NewGuid();
        var domainId = Guid.NewGuid();

        var summary = new CampaignSummaryBuilder()
            .WithCampaignId(campaignId)
            .WithDomainId(domainId)
            .WithCampaignValue("fluent-test")
            .WithTotalCaptured(42)
            .Build();

        summary.Should().NotBeNull();
        summary.CampaignId.Should().Be(campaignId);
        summary.TotalCaptured.Should().Be(42);
    }
}
