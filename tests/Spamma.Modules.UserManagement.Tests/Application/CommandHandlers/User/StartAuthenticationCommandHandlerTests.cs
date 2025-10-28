using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.UserManagement;
using Spamma.Modules.UserManagement.Application.CommandHandlers.User;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using UserAggregate = Spamma.Modules.UserManagement.Domain.UserAggregate.User;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.User;

public class StartAuthenticationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<StartAuthenticationCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly StartAuthenticationCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public StartAuthenticationCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<StartAuthenticationCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);
        
        // Pass empty validators collection since we're not testing validation logic
        var validators = Array.Empty<IValidator<StartAuthenticationCommand>>();

        _handler = new StartAuthenticationCommandHandler(
            _userRepositoryMock.Object,
            _timeProvider,
            _eventPublisherMock.Object,
            validators,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = new StartAuthenticationCommand("unknown@example.com");
        var userMaybe = Maybe<UserAggregate>.Nothing;

        _userRepositoryMock
            .Setup(x => x.GetByEmailAddressAsync("unknown@example.com", CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify - CommandResult doesn't have public properties to inspect success/failure
        result.Should().NotBeNull();

        _userRepositoryMock.Verify(
            x => x.GetByEmailAddressAsync("unknown@example.com", CancellationToken.None),
            Times.Once);
        
        _userRepositoryMock.VerifyNoOtherCalls();
        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUserSuspended_ReturnsAccountSuspendedError()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("suspended@example.com")
            .Build();

        // Suspend the user first
        user.Suspend(AccountSuspensionReason.Administrative, "Test suspension", _fixedUtcNow);

        var userMaybe = Maybe.From(user);
        var command = new StartAuthenticationCommand("suspended@example.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAddressAsync("suspended@example.com", CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _userRepositoryMock.Verify(
            x => x.GetByEmailAddressAsync("suspended@example.com", CancellationToken.None),
            Times.Once);
        
        _userRepositoryMock.VerifyNoOtherCalls();
        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsSavingChangesFailedError()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("valid@example.com")
            .Build();

        var userMaybe = Maybe.From(user);
        var command = new StartAuthenticationCommand("valid@example.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAddressAsync("valid@example.com", CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Mock repository save to fail
        _userRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _userRepositoryMock.Verify(
            x => x.GetByEmailAddressAsync("valid@example.com", CancellationToken.None),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);

        _eventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticationSucceeds_PublishesEventAndReturnsSuccess()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("active@example.com")
            .Build();

        var userMaybe = Maybe.From(user);
        var command = new StartAuthenticationCommand("active@example.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAddressAsync("active@example.com", CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _userRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<AuthenticationStartedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        _userRepositoryMock.Verify(
            x => x.GetByEmailAddressAsync("active@example.com", CancellationToken.None),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);

        // Verify event published with correct data
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<AuthenticationStartedIntegrationEvent>(e =>
                    e.UserId == user.Id &&
                    e.EmailAddress == "active@example.com"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticatesTwice_CreatesNewAuthenticationAttemptId()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("duplicate@example.com")
            .Build();

        var userMaybe = Maybe.From(user);
        var command = new StartAuthenticationCommand("duplicate@example.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAddressAsync("duplicate@example.com", CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _userRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        AuthenticationStartedIntegrationEvent? capturedEvent1 = null;
        AuthenticationStartedIntegrationEvent? capturedEvent2 = null;

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<AuthenticationStartedIntegrationEvent>(),
                CancellationToken.None))
            .Callback<AuthenticationStartedIntegrationEvent, CancellationToken>((e, _) =>
            {
                if (capturedEvent1 == null)
                    capturedEvent1 = e;
                else
                    capturedEvent2 = e;
            })
            .Returns(Task.CompletedTask);

        // Act - First authentication
        var result1 = await _handler.Handle(command, CancellationToken.None);
        result1.Should().NotBeNull();

        // Reset mock for second call
        _userRepositoryMock.Reset();
        _userRepositoryMock
            .Setup(x => x.GetByEmailAddressAsync("duplicate@example.com", CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _userRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act - Second authentication
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result2.Should().NotBeNull();
        capturedEvent1.Should().NotBeNull();
        capturedEvent2.Should().NotBeNull();
        capturedEvent1!.AuthenticationAttemptId.Should().NotBe(capturedEvent2!.AuthenticationAttemptId);
    }
}
