using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands.Campaign;

public class DeleteCampaignCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var command = new DeleteCampaignCommand(Guid.NewGuid());
        var authorizer = new DeleteCampaignCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsCampaignAccessRequirement()
    {
        // Arrange
        var command = new DeleteCampaignCommand(Guid.NewGuid());
        var authorizer = new DeleteCampaignCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToCampaignRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var command = new DeleteCampaignCommand(Guid.NewGuid());
        var authorizer = new DeleteCampaignCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentCampaignIds()
    {
        // Arrange
        var campaignId1 = Guid.NewGuid();
        var campaignId2 = Guid.NewGuid();
        var authorizer1 = new DeleteCampaignCommandAuthorizer();
        var authorizer2 = new DeleteCampaignCommandAuthorizer();

        // Act
        authorizer1.BuildPolicy(new DeleteCampaignCommand(campaignId1));
        authorizer2.BuildPolicy(new DeleteCampaignCommand(campaignId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}