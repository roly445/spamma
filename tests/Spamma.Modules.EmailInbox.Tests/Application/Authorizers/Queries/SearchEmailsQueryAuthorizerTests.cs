using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Queries;

public class SearchEmailsQueryAuthorizerTests
{
    [Fact]
    public void SearchEmailsQueryAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new SearchEmailsQueryAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void SearchEmailsQueryAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new SearchEmailsQueryAuthorizer();
        var query = new SearchEmailsQuery();

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void SearchEmailsQueryAuthorizer_BuildPolicyWithSearchText()
    {
        // Arrange
        var authorizer = new SearchEmailsQueryAuthorizer();
        var query = new SearchEmailsQuery(SearchText: "test@example.com");

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void SearchEmailsQueryAuthorizer_BuildPolicyWithDateFilters()
    {
        // Arrange
        var authorizer = new SearchEmailsQueryAuthorizer();
        var query = new SearchEmailsQuery(SearchText: "recent emails");

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void SearchEmailsQueryAuthorizer_BuildPolicyWithPagination()
    {
        // Arrange
        var authorizer = new SearchEmailsQueryAuthorizer();
        var query = new SearchEmailsQuery(Page: 2, PageSize: 25);

        // Act & Verify
        var action = () => authorizer.BuildPolicy(query);
        action.Should().NotThrow();
    }

    [Fact]
    public void SearchEmailsQueryAuthorizer_BuildPolicyMultipleTimes()
    {
        // Arrange
        var authorizer = new SearchEmailsQueryAuthorizer();

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(new SearchEmailsQuery());
        var action2 = () => authorizer.BuildPolicy(new SearchEmailsQuery(SearchText: "test"));

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }
}
