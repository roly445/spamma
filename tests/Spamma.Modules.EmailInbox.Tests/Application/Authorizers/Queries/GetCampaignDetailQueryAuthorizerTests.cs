using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class GetCampaignDetailQueryAuthorizerTests
{
    [Fact]
    public void GetCampaignDetailQueryAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new GetCampaignDetailQueryAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void GetCampaignDetailQueryAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new GetCampaignDetailQueryAuthorizer();
        var query = new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetCampaignDetailQueryAuthorizer_BuildPolicyWithValidIds()
    {
        // Arrange
        var authorizer = new GetCampaignDetailQueryAuthorizer();
        var subdomainId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        // Act & Verify
        var action = () => authorizer.BuildPolicy(new GetCampaignDetailQuery(subdomainId, campaignId));
        action.Should().NotThrow();
    }

    [Fact]
    public void GetCampaignDetailQueryAuthorizer_BuildPolicyWithOptionalParameters()
    {
        // Arrange
        var authorizer = new GetCampaignDetailQueryAuthorizer();

        // Act & Verify - BuildPolicy should handle optional DaysBucket parameter
        var action = () => authorizer.BuildPolicy(new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid(), 14));
        action.Should().NotThrow();
    }

    [Fact]
    public void GetCampaignDetailQueryAuthorizer_BuildPolicyMultipleTimes()
    {
        // Arrange
        var authorizer = new GetCampaignDetailQueryAuthorizer();

        // Act & Verify - Should be callable multiple times with different campaigns
        var action1 = () => authorizer.BuildPolicy(new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid()));
        var action2 = () => authorizer.BuildPolicy(new GetCampaignDetailQuery(Guid.NewGuid(), Guid.NewGuid()));

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }
}
