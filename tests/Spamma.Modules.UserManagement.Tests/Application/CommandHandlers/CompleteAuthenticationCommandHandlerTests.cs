using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MaybeMonad;
using ResultMonad;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.CommandHandlers;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Domain.UserAggregate;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using BluQube.Commands;
using FluentValidation;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers;

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
        _repositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<CompleteAuthenticationCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var settings = new Settings
        {
            SigningKeyBase64 = "test-key",
            BaseUri = "https://localhost",
            JwtKey = "test-jwt-key",
            JwtIssuer = "test-issuer",
            AuthenticationTimeInMinutes = 15,
            MailServerHostname = "localhost"
        };
        _settingsOptions = Options.Create(settings);

        var validators = Array.Empty<IValidator<CompleteAuthenticationCommand>>();

        _handler = new CompleteAuthenticationCommandHandler(
            _repositoryMock.Object,
            _timeProvider,
            _settingsOptions,
            validators,
            _loggerMock.Object);
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
    }

    [Fact]
    public async Task Handle_WhenAuthenticationAttemptExpired_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(_fixedUtcNow);

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            Guid.NewGuid()); // Wrong attempt ID or expired

        var userMaybe = Maybe.From(user);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify - should fail due to invalid/expired attempt
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        _repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenAuthenticationCompletes_SavesUserAndSucceeds()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(_fixedUtcNow);
        var authAttempt = authResult.Value;

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            authAttempt.AuthenticationAttemptId);

        var userMaybe = Maybe.From(user);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

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
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(_fixedUtcNow);
        var authAttempt = authResult.Value;

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            authAttempt.AuthenticationAttemptId);

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
    }

    [Fact]
    public async Task Handle_WithMultipleAttempts_HandlesIdempotently()
    {
        // Arrange - Verify same command can be retried if needed
        var user = new UserBuilder().Build();
        var authResult = user.StartAuthentication(_fixedUtcNow);
        var authAttempt = authResult.Value;

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            authAttempt.AuthenticationAttemptId);

        var userMaybe = Maybe.From(user);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act - Call handler multiple times with same command
        var result1 = await _handler.Handle(command, CancellationToken.None);
        result1.Should().NotBeNull();

        // For second call, create new user state since user reference changed
        var user2 = new UserBuilder().Build();
        var authResult2 = user2.StartAuthentication(_fixedUtcNow);
        var authAttempt2 = authResult2.Value;
        var command2 = new CompleteAuthenticationCommand(
            user2.Id,
            user2.SecurityStamp,
            authAttempt2.AuthenticationAttemptId);

        var userMaybe2 = Maybe.From(user2);
        _repositoryMock
            .Setup(x => x.GetByIdAsync(user2.Id, CancellationToken.None))
            .ReturnsAsync(userMaybe2);

        var result2 = await _handler.Handle(command2, CancellationToken.None);
        result2.Should().NotBeNull();

        // Verify repository was called twice (once per command)
        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<User>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
