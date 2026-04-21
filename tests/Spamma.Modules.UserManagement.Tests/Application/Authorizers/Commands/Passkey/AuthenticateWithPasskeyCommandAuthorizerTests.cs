using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.Passkey;

public class AuthenticateWithPasskeyCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsNotAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new AuthenticateWithPasskeyCommandAuthorizer();
        var command = MakeCommand([1, 2, 3], 0);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustNotBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new AuthenticateWithPasskeyCommandAuthorizer();
        var command = MakeCommand([1, 2, 3], 0);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_WithDifferentCommands_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new AuthenticateWithPasskeyCommandAuthorizer();
        var authorizer2 = new AuthenticateWithPasskeyCommandAuthorizer();
        var command1 = MakeCommand([1, 2, 3], 0);
        var command2 = MakeCommand([10, 11, 12], 1);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(1);
        authorizer2.Requirements.Should().HaveCount(1);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustNotBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustNotBeAuthenticatedRequirement");
    }

    private static AuthenticateWithPasskeyCommand MakeCommand(byte[] credentialId, uint signCount) =>
        new(credentialId, signCount, new byte[37], new byte[1], new byte[1], "challenge==", "https://localhost", "localhost");
}