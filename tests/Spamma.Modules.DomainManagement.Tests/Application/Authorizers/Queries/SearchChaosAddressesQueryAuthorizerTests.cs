using FluentAssertions;

using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

public class SearchChaosAddressesQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_WhenSubdomainIdProvided_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new SearchChaosAddressesQuery(SubdomainId: Guid.NewGuid());
        var authorizer = new SearchChaosAddressesQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_WhenSubdomainIdProvided_AddsSubdomainAccessRequirement()
    {
        // Arrange
        var query = new SearchChaosAddressesQuery(SubdomainId: Guid.NewGuid());
        var authorizer = new SearchChaosAddressesQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_WhenSubdomainIdNull_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new SearchChaosAddressesQuery(SubdomainId: null);
        var authorizer = new SearchChaosAddressesQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_WhenSubdomainIdNull_AddsAtLeastOneSubdomainModeratorRequirement()
    {
        // Arrange
        var query = new SearchChaosAddressesQuery(SubdomainId: null);
        var authorizer = new SearchChaosAddressesQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToAtLeastOneSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_AlwaysRequiresExactlyTwoRequirements()
    {
        // Arrange
        var authorizerWithSubdomain = new SearchChaosAddressesQueryAuthorizer();
        var authorizerWithoutSubdomain = new SearchChaosAddressesQueryAuthorizer();

        // Act
        authorizerWithSubdomain.BuildPolicy(new SearchChaosAddressesQuery(SubdomainId: Guid.NewGuid()));
        authorizerWithoutSubdomain.BuildPolicy(new SearchChaosAddressesQuery(SubdomainId: null));

        // Assert
        authorizerWithSubdomain.Requirements.Should().HaveCount(2);
        authorizerWithoutSubdomain.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var authorizer1 = new SearchChaosAddressesQueryAuthorizer();
        var authorizer2 = new SearchChaosAddressesQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new SearchChaosAddressesQuery(SubdomainId: subdomainId1));
        authorizer2.BuildPolicy(new SearchChaosAddressesQuery(SubdomainId: subdomainId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
    }
}