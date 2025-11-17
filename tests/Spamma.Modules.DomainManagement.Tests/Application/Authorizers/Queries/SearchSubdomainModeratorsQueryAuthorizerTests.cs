using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

public class SearchSubdomainModeratorsQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new SearchSubdomainModeratorsQuery(Guid.NewGuid());
        var authorizer = new SearchSubdomainModeratorsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsSubdomainModeratorRequirement()
    {
        // Arrange
        var query = new SearchSubdomainModeratorsQuery(Guid.NewGuid());
        var authorizer = new SearchSubdomainModeratorsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new SearchSubdomainModeratorsQuery(Guid.NewGuid());
        var authorizer = new SearchSubdomainModeratorsQueryAuthorizer();

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
        var authorizer1 = new SearchSubdomainModeratorsQueryAuthorizer();
        var authorizer2 = new SearchSubdomainModeratorsQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new SearchSubdomainModeratorsQuery(subdomainId1));
        authorizer2.BuildPolicy(new SearchSubdomainModeratorsQuery(subdomainId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}