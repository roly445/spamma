using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Queries;

public class SearchDomainsQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new SearchDomainsQuery(null, null, null, 1, 10, "Name", false);
        var authorizer = new SearchDomainsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsDomainModeratorRequirement()
    {
        // Arrange
        var query = new SearchDomainsQuery(null, null, null, 1, 10, "Name", false);
        var authorizer = new SearchDomainsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToAtLeastOneDomainRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new SearchDomainsQuery(null, null, null, 1, 10, "Name", false);
        var authorizer = new SearchDomainsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentQueryParameters()
    {
        // Arrange
        var authorizer1 = new SearchDomainsQueryAuthorizer();
        var authorizer2 = new SearchDomainsQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new SearchDomainsQuery(null, null, null, Page: 1, PageSize: 10, SortBy: "Name", SortDescending: false));
        authorizer2.BuildPolicy(new SearchDomainsQuery(null, null, null, Page: 2, PageSize: 50, SortBy: "CreatedAt", SortDescending: true));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}