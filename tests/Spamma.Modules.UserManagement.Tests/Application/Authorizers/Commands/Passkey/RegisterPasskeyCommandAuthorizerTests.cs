using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.Passkey;

public class RegisterPasskeyCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new RegisterPasskeyCommandAuthorizer();
        var command = new RegisterPasskeyCommand(
            [1, 2, 3],
            [4, 5, 6],
            0,
            "My Passkey",
            "ES256");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new RegisterPasskeyCommandAuthorizer();
        var command = new RegisterPasskeyCommand(
            [1, 2, 3],
            [4, 5, 6],
            0,
            "My Passkey",
            "ES256");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_WithDifferentCommands_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new RegisterPasskeyCommandAuthorizer();
        var authorizer2 = new RegisterPasskeyCommandAuthorizer();
        var command1 = new RegisterPasskeyCommand([1, 2, 3], [4, 5, 6], 0, "Passkey1", "ES256");
        var command2 = new RegisterPasskeyCommand([7, 8, 9], [10, 11, 12], 1, "Passkey2", "RS256");

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(1);
        authorizer2.Requirements.Should().HaveCount(1);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }
}
