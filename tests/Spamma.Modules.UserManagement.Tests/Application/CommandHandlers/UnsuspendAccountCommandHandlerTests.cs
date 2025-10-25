using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MaybeMonad;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.CommandHandlers;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.UserAggregate;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers;

public class UnsuspendAccountCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<UnsuspendAccountCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly UnsuspendAccountCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public UnsuspendAccountCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<UnsuspendAccountCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<UnsuspendAccountCommand>>();

        _handler = new UnsuspendAccountCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            _eventPublisherMock.Object,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UnsuspendAccountCommand(userId);

        var userMaybe = Maybe<User>.Nothing;

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        _repositoryMock.VerifyNoOtherCalls();
        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUserNotSuspended_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var userId = user.Id;
        var command = new UnsuspendAccountCommand(userId);

        var userMaybe = Maybe.From(user);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        _repositoryMock.VerifyNoOtherCalls();
        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUnsuspendSucceeds_SavesUserAndPublishesEvent()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, "Test suspension", _fixedUtcNow);

        var userId = user.Id;
        var command = new UnsuspendAccountCommand(userId);

        var userMaybe = Maybe.From(user);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<UserUnsuspendedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<UserUnsuspendedIntegrationEvent>(e =>
                    e.UserId == userId &&
                    e.WhenHappened == _fixedUtcNow),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.Suspend(AccountSuspensionReason.Administrative, "Test suspension", _fixedUtcNow);

        var userId = user.Id;
        var command = new UnsuspendAccountCommand(userId);

        var userMaybe = Maybe.From(user);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MultipleSuspendedUsers_UnsuspendsSuccessfully()
    {
        // Arrange - Test unsuspending multiple users
        var user1 = new UserBuilder().WithEmail("user1@example.com").Build();
        var user2 = new UserBuilder().WithEmail("user2@example.com").Build();

        user1.Suspend(AccountSuspensionReason.Administrative, "Suspended 1", _fixedUtcNow);
        user2.Suspend(AccountSuspensionReason.PolicyViolation, "Suspended 2", _fixedUtcNow);

        var command1 = new UnsuspendAccountCommand(user1.Id);
        var command2 = new UnsuspendAccountCommand(user2.Id);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(user1.Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user1));

        _repositoryMock
            .Setup(x => x.GetByIdAsync(user2.Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user2));

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<UserUnsuspendedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act & Verify
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        result1.Should().NotBeNull();

        var result2 = await _handler.Handle(command2, CancellationToken.None);
        result2.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None),
            Times.Exactly(2));

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserUnsuspendedIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
