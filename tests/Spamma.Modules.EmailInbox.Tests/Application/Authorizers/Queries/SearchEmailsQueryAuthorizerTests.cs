using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

/// <summary>
/// Tests for SearchEmailsQueryAuthorizer to verify authorization requirements.
/// </summary>
public class SearchEmailsQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var query = new SearchEmailsQuery();
        var authorizer = new SearchEmailsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsSubdomainAccessRequirement()
    {
        // Arrange
        var query = new SearchEmailsQuery();
        var authorizer = new SearchEmailsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var query = new SearchEmailsQuery();
        var authorizer = new SearchEmailsQueryAuthorizer();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentQueryParameters()
    {
        // Arrange
        var authorizer1 = new SearchEmailsQueryAuthorizer();
        var authorizer2 = new SearchEmailsQueryAuthorizer();

        // Act
        authorizer1.BuildPolicy(new SearchEmailsQuery(SearchText: "test", Page: 1, PageSize: 20));
        authorizer2.BuildPolicy(new SearchEmailsQuery(SearchText: null, Page: 2, PageSize: 50));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}
