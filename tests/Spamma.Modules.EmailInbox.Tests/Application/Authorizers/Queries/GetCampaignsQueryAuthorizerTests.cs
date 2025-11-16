using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class GetCampaignsQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new GetCampaignsQuery(Guid.NewGuid());
        var authorizer = new GetCampaignsQueryAuthorizer();

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
        var query = new GetCampaignsQuery(Guid.NewGuid());
        var authorizer = new GetCampaignsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToAtLeastOneCampaignRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new GetCampaignsQuery(Guid.NewGuid());
        var authorizer = new GetCampaignsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var authorizer1 = new GetCampaignsQueryAuthorizer();
        var authorizer2 = new GetCampaignsQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new GetCampaignsQuery(subdomainId1));
        authorizer2.BuildPolicy(new GetCampaignsQuery(subdomainId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}