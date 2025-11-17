using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToCampaignRequirementTests
{
    [Fact]
    public void Requirement_WithValidCampaignId_CreatesSuccessfully()
    {
        var campaignId = Guid.NewGuid();

        var requirement = new MustHaveAccessToCampaignRequirement
        {
            CampaignId = campaignId,
        };

        requirement.Should().NotBeNull();
        requirement.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public void Requirement_WithEmptyCampaignId_IsAllowed()
    {
        var requirement = new MustHaveAccessToCampaignRequirement
        {
            CampaignId = Guid.Empty,
        };

        requirement.Should().NotBeNull();
    }
}