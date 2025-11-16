using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class SuspendAccountCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new SuspendAccountCommandAuthorizer();
        var userId = Guid.NewGuid();
        var command = new SuspendAccountCommand(userId, AccountSuspensionReason.PolicyViolation, "Violates terms");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsUserAdministratorRequirement()
    {
        // Arrange
        var authorizer = new SuspendAccountCommandAuthorizer();
        var userId = Guid.NewGuid();
        var command = new SuspendAccountCommand(userId, AccountSuspensionReason.PolicyViolation, "Violates terms");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeUserAdministratorRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyTwoRequirements()
    {
        // Arrange
        var authorizer = new SuspendAccountCommandAuthorizer();
        var userId = Guid.NewGuid();
        var command = new SuspendAccountCommand(userId, AccountSuspensionReason.PolicyViolation, "Violates terms");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WithDifferentUserIds_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new SuspendAccountCommandAuthorizer();
        var authorizer2 = new SuspendAccountCommandAuthorizer();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var command1 = new SuspendAccountCommand(userId1, AccountSuspensionReason.PolicyViolation, "Reason 1");
        var command2 = new SuspendAccountCommand(userId2, AccountSuspensionReason.SpamAbuse, null);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeUserAdministratorRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeUserAdministratorRequirement");
    }
}