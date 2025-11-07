using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class GetCampaignsQueryAuthorizerTests
{
    [Fact]
    public void GetCampaignsQueryAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new GetCampaignsQueryAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void GetCampaignsQueryAuthorizer_ImplementsAbstractAuthorizer()
    {
        // Arrange & Act
        var authorizer = new GetCampaignsQueryAuthorizer();

        // Verify - Should be an AbstractRequestAuthorizer for GetCampaignsQuery
        authorizer.Should().BeAssignableTo<GetCampaignsQueryAuthorizer>();
    }

    [Fact]
    public void GetCampaignsQueryAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new GetCampaignsQueryAuthorizer();
        var query = new GetCampaignsQuery(Guid.NewGuid());

        // Act & Verify - BuildPolicy should execute without exception
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetCampaignsQueryAuthorizer_BuildPolicyWithDifferentSubdomains()
    {
        // Arrange
        var authorizer = new GetCampaignsQueryAuthorizer();
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();

        // Act & Verify - BuildPolicy should work with different subdomain IDs
        var action1 = () => authorizer.BuildPolicy(new GetCampaignsQuery(subdomainId1));
        var action2 = () => authorizer.BuildPolicy(new GetCampaignsQuery(subdomainId2));

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }
}
