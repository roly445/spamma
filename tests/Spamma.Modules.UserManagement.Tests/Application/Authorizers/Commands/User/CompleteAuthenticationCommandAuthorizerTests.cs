using FluentAssertions;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class CompleteAuthenticationCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsNotAuthenticatedRequirement()
    {
        // Arrange
        var authorizer = new CompleteAuthenticationCommandAuthorizer();
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();
        var command = new CompleteAuthenticationCommand(userId, securityStamp, authenticationAttemptId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustNotBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_AddsExactlyOneRequirement()
    {
        // Arrange
        var authorizer = new CompleteAuthenticationCommandAuthorizer();
        var userId = Guid.NewGuid();
        var securityStamp = Guid.NewGuid();
        var authenticationAttemptId = Guid.NewGuid();
        var command = new CompleteAuthenticationCommand(userId, securityStamp, authenticationAttemptId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPolicy_WithDifferentIds_ProducesSameRequirements()
    {
        // Arrange
        var authorizer1 = new CompleteAuthenticationCommandAuthorizer();
        var authorizer2 = new CompleteAuthenticationCommandAuthorizer();
        var userId1 = Guid.NewGuid();
        var securityStamp1 = Guid.NewGuid();
        var authenticationAttemptId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var securityStamp2 = Guid.NewGuid();
        var authenticationAttemptId2 = Guid.NewGuid();
        var command1 = new CompleteAuthenticationCommand(userId1, securityStamp1, authenticationAttemptId1);
        var command2 = new CompleteAuthenticationCommand(userId2, securityStamp2, authenticationAttemptId2);

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