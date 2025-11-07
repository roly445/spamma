using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.CommandHandlers.User;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using UserAggregate = Spamma.Modules.UserManagement.Domain.UserAggregate.User;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.User;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<CreateUserCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly CreateUserCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public CreateUserCommandHandlerTests()
    {
        this._repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<CreateUserCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<CreateUserCommand>>();

        this._handler = new CreateUserCommandHandler(
            this._repositoryMock.Object,
            this._eventPublisherMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserCreationSucceeds_SavesUserAndPublishesEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(
            userId,
            "John Doe",
            "john@example.com",
            true,
            SystemRole.UserManagement);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<UserCreatedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<UserAggregate>(u =>
                    u.Id == userId &&
                    u.Name == "John Doe" &&
                    u.EmailAddress == "john@example.com"),
                CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<UserCreatedIntegrationEvent>(e =>
                    e.UserId == userId &&
                    e.Name == "John Doe" &&
                    e.EmailAddress == "john@example.com" &&
                    e.SendWelcome),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(
            userId,
            "Jane Doe",
            "jane@example.com",
            false,
            SystemRole.DomainManagement);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_PublishesEventWithCorrectTimeestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateUserCommand(
            userId,
            "Bob Smith",
            "bob@example.com",
            true,
            SystemRole.UserManagement);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        UserCreatedIntegrationEvent? capturedEvent = null;
        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<UserCreatedIntegrationEvent>(),
                CancellationToken.None))
            .Callback<UserCreatedIntegrationEvent, CancellationToken>((e, _) =>
            {
                capturedEvent = e;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.WhenHappened.Should().Be(this._fixedUtcNow);
    }

    [Fact]
    public async Task Handle_WithDifferentSystemRoles_CreatesUsersSuccessfully()
    {
        // Arrange
        var userIdAdmin = Guid.NewGuid();
        var userIdMod = Guid.NewGuid();

        var commandAdmin = new CreateUserCommand(
            userIdAdmin,
            "Admin User",
            "admin@example.com",
            true,
            SystemRole.UserManagement);

        var commandMod = new CreateUserCommand(
            userIdMod,
            "Regular User",
            "user@example.com",
            false,
            SystemRole.DomainManagement);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<UserCreatedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act & Verify
        var result1 = await this._handler.Handle(commandAdmin, CancellationToken.None);
        result1.Should().NotBeNull();

        var result2 = await this._handler.Handle(commandMod, CancellationToken.None);
        result2.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Exactly(2));

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<UserCreatedIntegrationEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}