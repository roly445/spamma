using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

public class GetChaosAddressBySubdomainAndLocalPartQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer();
        var subdomainId = Guid.NewGuid();
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(subdomainId, "test");

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsAccessToSubdomainRequirement()
    {
        // Arrange
        var authorizer = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer();
        var subdomainId = Guid.NewGuid();
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(subdomainId, "test");

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var authorizer1 = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer();
        var authorizer2 = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer();
        var query1 = new GetChaosAddressBySubdomainAndLocalPartQuery(subdomainId1, "test1");
        var query2 = new GetChaosAddressBySubdomainAndLocalPartQuery(subdomainId2, "test2");

        // Act
        authorizer1.BuildPolicy(query1);
        authorizer2.BuildPolicy(query2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}
