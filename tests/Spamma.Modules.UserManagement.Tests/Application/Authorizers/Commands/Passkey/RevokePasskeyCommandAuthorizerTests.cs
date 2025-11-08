using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.Passkey;

public class RevokePasskeyCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new RevokePasskeyCommandAuthorizer();
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new RevokePasskeyCommandAuthorizer();
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_WithDifferentPasskeyIds_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new RevokePasskeyCommandAuthorizer();
        var authorizer2 = new RevokePasskeyCommandAuthorizer();
        var passkeyId1 = Guid.NewGuid();
        var passkeyId2 = Guid.NewGuid();
        var command1 = new RevokePasskeyCommand(passkeyId1);
        var command2 = new RevokePasskeyCommand(passkeyId2);

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
