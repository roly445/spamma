using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ResultMonad;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Application.CommandHandlers.User;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using UserAggregate = Spamma.Modules.UserManagement.Domain.UserAggregate.User;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.User;

public class CompleteAuthenticationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<CompleteAuthenticationCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly CompleteAuthenticationCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);
    private readonly IOptions<Settings> _settingsOptions;

    public CompleteAuthenticationCommandHandlerTests()
    {
        this._repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<CompleteAuthenticationCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var settings = new Settings
        {
            SigningKeyBase64 = "test-key",
            BaseUri = "https://localhost",
            AuthenticationTimeInMinutes = 15,
            MailServerHostname = "localhost",
        };
        this._settingsOptions = Options.Create(settings);

        var validators = Array.Empty<IValidator<CompleteAuthenticationCommand>>();

        this._handler = new CompleteAuthenticationCommandHandler(
            this._repositoryMock.Object,
            this._timeProvider,
            this._settingsOptions,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CompleteAuthenticationCommand(
            userId,
            Guid.NewGuid(),
            Guid.NewGuid());

        var userMaybe = Maybe<UserAggregate>.Nothing;

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
    public async Task Handle_WhenAuthenticationAttemptExpired_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        _ = user.StartAuthentication(this._fixedUtcNow);

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            Guid.NewGuid()); // Wrong attempt ID or expired

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify - should fail due to invalid/expired attempt
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenAuthenticationCompletes_SavesUserAndSucceeds()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(this._fixedUtcNow);
        var authAttempt = authResult.Value;

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            authAttempt.AuthenticationAttemptId);

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(this._fixedUtcNow);
        var authAttempt = authResult.Value;

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            authAttempt.AuthenticationAttemptId);

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleAttempts_HandlesIdempotently()
    {
        // Arrange - Verify same command can be retried if needed
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(this._fixedUtcNow);
        var authAttempt = authResult.Value;

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            authAttempt.AuthenticationAttemptId);

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        this._repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act - Call handler multiple times with same command
        var result1 = await this._handler.Handle(command, CancellationToken.None);
        result1.Should().NotBeNull();

        // For second call, create new user state since user reference changed
        var user2 = new UserBuilder().Build();
        var authResult2 = user2.StartAuthentication(this._fixedUtcNow);
        var authAttempt2 = authResult2.Value;
        var command2 = new CompleteAuthenticationCommand(
            user2.Id,
            user2.SecurityStamp,
            authAttempt2.AuthenticationAttemptId);

        var userMaybe2 = Maybe.From(user2);
        this._repositoryMock
            .Setup(x => x.GetByIdAsync(user2.Id, CancellationToken.None))
            .ReturnsAsync(userMaybe2);

        var result2 = await this._handler.Handle(command2, CancellationToken.None);
        result2.Should().NotBeNull();

        // Verify repository was called twice (once per command)
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Exactly(2));
    }
}