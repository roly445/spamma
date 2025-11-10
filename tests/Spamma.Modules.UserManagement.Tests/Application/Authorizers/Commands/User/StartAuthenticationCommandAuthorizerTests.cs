using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class StartAuthenticationCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsNotAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new StartAuthenticationCommandAuthorizer();
        var command = new StartAuthenticationCommand("user@example.com");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustNotBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new StartAuthenticationCommandAuthorizer();
        var command = new StartAuthenticationCommand("user@example.com");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_WithDifferentEmails_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new StartAuthenticationCommandAuthorizer();
        var authorizer2 = new StartAuthenticationCommandAuthorizer();
        var command1 = new StartAuthenticationCommand("user1@example.com");
        var command2 = new StartAuthenticationCommand("user2@example.com");

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(1);
        authorizer2.Requirements.Should().HaveCount(1);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustNotBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustNotBeAuthenticatedRequirement");
    }
}
