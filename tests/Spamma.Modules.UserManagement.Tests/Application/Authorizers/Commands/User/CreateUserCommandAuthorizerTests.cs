using FluentAssertions;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class CreateUserCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_WhenNotInternalQuery_AddsAuthenticatedRequirement()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(userId, "John Doe", "john@example.com", false, SystemRole.UserManagement);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(command))
            .Returns(false);

        var authorizer = new CreateUserCommandAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeAuthenticatedRequirement);
    }

    [Fact]
    public void BuildPolicy_WhenNotInternalQuery_AddsUserAdministratorRequirement()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(userId, "John Doe", "john@example.com", false, SystemRole.UserManagement);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(command))
            .Returns(false);

        var authorizer = new CreateUserCommandAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().ContainSingle(r => r is MustBeUserAdministratorRequirement);
    }

    [Fact]
    public void BuildPolicy_WhenNotInternalQuery_AddsExactlyTwoRequirements()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(userId, "John Doe", "john@example.com", false, SystemRole.UserManagement);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(command))
            .Returns(false);

        var authorizer = new CreateUserCommandAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WhenInternalQuery_AddsNoRequirements()
    {
        // Arrange
        var internalQueryStoreMock = new Mock<IInternalQueryStore>();
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(userId, "John Doe", "john@example.com", false, SystemRole.UserManagement);

        internalQueryStoreMock
            .Setup(x => x.IsQueryStored(command))
            .Returns(true);

        var authorizer = new CreateUserCommandAuthorizer(internalQueryStoreMock.Object);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void BuildPolicy_WithDifferentUserIds_ProducesSameRequirements()
    {
        // Arrange
        var internalQueryStoreMock1 = new Mock<IInternalQueryStore>();
        var internalQueryStoreMock2 = new Mock<IInternalQueryStore>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var command1 = new CreateUserCommand(userId1, "User1", "user1@example.com", false, SystemRole.UserManagement);
        var command2 = new CreateUserCommand(userId2, "User2", "user2@example.com", true, SystemRole.DomainManagement);

        internalQueryStoreMock1
            .Setup(x => x.IsQueryStored(command1))
            .Returns(false);

        internalQueryStoreMock2
            .Setup(x => x.IsQueryStored(command2))
            .Returns(false);

        var authorizer1 = new CreateUserCommandAuthorizer(internalQueryStoreMock1.Object);
        var authorizer2 = new CreateUserCommandAuthorizer(internalQueryStoreMock2.Object);

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
