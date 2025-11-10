using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetUserPasskeysQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new GetUserPasskeysQueryAuthorizer();
        var userId = Guid.NewGuid();
        var query = new GetUserPasskeysQuery(userId);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsUserAdministratorRequirement()
    {
        // Arrange
        var authorizer = new GetUserPasskeysQueryAuthorizer();
        var userId = Guid.NewGuid();
        var query = new GetUserPasskeysQuery(userId);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeUserAdministratorRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyTwoRequirements()
    {
        // Arrange
        var authorizer = new GetUserPasskeysQueryAuthorizer();
        var userId = Guid.NewGuid();
        var query = new GetUserPasskeysQuery(userId);

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WithDifferentUserIds_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new GetUserPasskeysQueryAuthorizer();
        var authorizer2 = new GetUserPasskeysQueryAuthorizer();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var query1 = new GetUserPasskeysQuery(userId1);
        var query2 = new GetUserPasskeysQuery(userId2);

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
