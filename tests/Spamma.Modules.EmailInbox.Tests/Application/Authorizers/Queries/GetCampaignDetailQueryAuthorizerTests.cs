using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

/// <summary>
/// Tests for GetCampaignDetailQueryAuthorizer to verify authorization requirements.
/// </summary>
public class GetCampaignDetailQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid());
        var authorizer = new GetCampaignDetailQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsCampaignAccessRequirement()
    {
        // Arrange
        var query = new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid());
        var authorizer = new GetCampaignDetailQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToCampaignRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid());
        var authorizer = new GetCampaignDetailQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentCampaignIds()
    {
        // Arrange
        var campaignId1 = Guid.NewGuid();
        var campaignId2 = Guid.NewGuid();
        var authorizer1 = new GetCampaignDetailQueryAuthorizer();
        var authorizer2 = new GetCampaignDetailQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new GetCampaignDetailQuery(Guid.NewGuid(), campaignId1));
        authorizer2.BuildPolicy(new GetCampaignDetailQuery(Guid.NewGuid(), campaignId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}
