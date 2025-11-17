using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class SearchUsersQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new SearchUsersQueryAuthorizer();
        var query = new SearchUsersQuery();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsUserAdministratorRequirement()
    {
        // Arrange
        var authorizer = new SearchUsersQueryAuthorizer();
        var query = new SearchUsersQuery();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeUserAdministratorRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyTwoRequirements()
    {
        // Arrange
        var authorizer = new SearchUsersQueryAuthorizer();
        var query = new SearchUsersQuery();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WithDifferentQueryParameters_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new SearchUsersQueryAuthorizer();
        var authorizer2 = new SearchUsersQueryAuthorizer();
        var query1 = new SearchUsersQuery("test1");
        var query2 = new SearchUsersQuery("test2", null, null, null, null, "Email", false, 2, 20);

        // Act
        authorizer1.BuildPolicy(query1);
        authorizer2.BuildPolicy(query2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeUserAdministratorRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeUserAdministratorRequirement");
    }
}