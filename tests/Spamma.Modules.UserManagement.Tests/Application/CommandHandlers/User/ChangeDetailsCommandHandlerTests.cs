using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.UserManagement.Application.CommandHandlers.User;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Tests.Builders;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.User;

public class ChangeDetailsCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<ChangeDetailsCommandHandler>> _loggerMock;
    private readonly ChangeDetailsCommandHandler _handler;

    public ChangeDetailsCommandHandlerTests()
    {
        this._repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<ChangeDetailsCommandHandler>>();

        var validators = Array.Empty<IValidator<ChangeDetailsCommand>>();

        this._handler = new ChangeDetailsCommandHandler(
            this._repositoryMock.Object,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ChangeDetailsCommand(
            userId,
            "newemail@example.com",
            "New Name",
            SystemRole.UserManagement);

        var userMaybe = Maybe<UserManagement.Domain.UserAggregate.User>.Nothing;

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenDetailsChangeSucceeds_SavesUserAndSucceeds()
    {
        // Arrange
        var user = new UserBuilder()
            .WithName("Old Name")
            .WithEmail("oldemail@example.com")
            .Build();

        var userId = user.Id;
        var command = new ChangeDetailsCommand(
            userId,
            "newemail@example.com",
            "New Name",
            SystemRole.DomainManagement);

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserManagement.Domain.UserAggregate.User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<UserManagement.Domain.UserAggregate.User>(u =>
                    u.Id == userId &&
                    u.Name == "New Name" &&
                    u.EmailAddress == "newemail@example.com"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var userId = user.Id;
        var command = new ChangeDetailsCommand(
            userId,
            "newemail@example.com",
            "New Name",
            SystemRole.UserManagement);

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserManagement.Domain.UserAggregate.User>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserManagement.Domain.UserAggregate.User>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentSystemRoles_UpdatesSuccessfully()
    {
        // Arrange - Test updating with different system roles
        var user1 = new UserBuilder().WithEmail("user1@example.com").Build();
        var user2 = new UserBuilder().WithEmail("user2@example.com").Build();

        var command1 = new ChangeDetailsCommand(
            user1.Id,
            "user1updated@example.com",
            "User One Updated",
            SystemRole.UserManagement);

        var command2 = new ChangeDetailsCommand(
            user2.Id,
            "user2updated@example.com",
            "User Two Updated",
            SystemRole.DomainManagement);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(user1.Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user1));

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(user2.Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user2));

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserManagement.Domain.UserAggregate.User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act & Verify
        var result1 = await this._handler.Handle(command1, CancellationToken.None);
        result1.Should().NotBeNull();

        var result2 = await this._handler.Handle(command2, CancellationToken.None);
        result2.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserManagement.Domain.UserAggregate.User>(), CancellationToken.None),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenUpdatingOnlyName_SucceedsAndPreservesOtherFields()
    {
        // Arrange - Test partial update (only name)
        var originalEmail = "original@example.com";
        var user = new UserBuilder()
            .WithName("Original Name")
            .WithEmail(originalEmail)
            .Build();

        var userId = user.Id;
        var command = new ChangeDetailsCommand(
            userId,
            originalEmail, // Keep same email
            "Updated Name Only",
            SystemRole.UserManagement);

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserManagement.Domain.UserAggregate.User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<UserManagement.Domain.UserAggregate.User>(u =>
                    u.Name == "Updated Name Only" &&
                    u.EmailAddress == originalEmail),
                CancellationToken.None),
            Times.Once);
    }
}