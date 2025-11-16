using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetUserStatsQueryAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new GetUserStatsQueryAuthorizer();
        var query = new GetUserStatsQuery();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new GetUserStatsQueryAuthorizer();
        var query = new GetUserStatsQuery();

        // Act
        authorizer.BuildPolicy(query);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_CalledWithDifferentInstances_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new GetUserStatsQueryAuthorizer();
        var authorizer2 = new GetUserStatsQueryAuthorizer();
        var query1 = new GetUserStatsQuery();
        var query2 = new GetUserStatsQuery();

        // Act
        authorizer1.BuildPolicy(query1);
        authorizer2.BuildPolicy(query2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(1);
        authorizer2.Requirements.Should().HaveCount(1);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }
}