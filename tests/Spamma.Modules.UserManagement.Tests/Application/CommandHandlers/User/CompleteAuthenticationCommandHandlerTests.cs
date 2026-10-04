using BluQube.Constants;
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
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Client.Contracts;
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
        result.Status.Should().Be(CommandResultStatus.Failed);

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenAuthenticationAttemptDoesNotExist_ReturnsError()
    {
        // Arrange
        var user = new UserBuilder().Build();
        _ = user.StartAuthentication(this._fixedUtcNow);

        var userId = user.Id;
        var command = new CompleteAuthenticationCommand(
            userId,
            user.SecurityStamp,
            Guid.NewGuid());

        var userMaybe = Maybe.From(user);

        this._repositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(userMaybe);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(CommandResultStatus.Failed);

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenAuthenticationAttemptExpired_RejectsAndPersistsFailure()
    {
        var user = new UserBuilder().Build();
        var attemptId = user.StartAuthentication(this._fixedUtcNow.AddMinutes(-16)).Value.AuthenticationAttemptId;
        var command = new CompleteAuthenticationCommand(user.Id, user.SecurityStamp, attemptId);

        this._repositoryMock.Setup(x => x.GetByIdAsync(user.Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user));
        this._repositoryMock.Setup(x => x.SaveAsync(user, CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        var result = await this._handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(CommandResultStatus.Failed);
        user.AuthenticationAttempts.Single(x => x.Id == attemptId).HasFinalized.Should().BeTrue();
        this._repositoryMock.Verify(x => x.SaveAsync(user, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSecurityStampChanged_RejectsAndPersistsFailure()
    {
        var user = new UserBuilder().Build();
        var attemptId = user.StartAuthentication(this._fixedUtcNow).Value.AuthenticationAttemptId;
        var previousSecurityStamp = user.SecurityStamp;
        user.Suspend(AccountSuspensionReason.Administrative, "Test suspension", this._fixedUtcNow);
        var command = new CompleteAuthenticationCommand(user.Id, previousSecurityStamp, attemptId);

        this._repositoryMock.Setup(x => x.GetByIdAsync(user.Id, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user));
        this._repositoryMock.Setup(x => x.SaveAsync(user, CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        var result = await this._handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(CommandResultStatus.Failed);
        user.AuthenticationAttempts.Single(x => x.Id == attemptId).HasFinalized.Should().BeTrue();
        this._repositoryMock.Verify(x => x.SaveAsync(user, CancellationToken.None), Times.Once);
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
        result.Status.Should().Be(CommandResultStatus.Succeeded);

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
        result.Status.Should().Be(CommandResultStatus.Failed);

        this._repositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);

        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAuthenticationAttemptAlreadyCompleted_RejectsReuse()
    {
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

        var result1 = await this._handler.Handle(command, CancellationToken.None);
        var result2 = await this._handler.Handle(command, CancellationToken.None);

        result1.Status.Should().Be(CommandResultStatus.Succeeded);
        result2.Status.Should().Be(CommandResultStatus.Failed);
        this._repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<UserAggregate>(), CancellationToken.None),
            Times.Once);
    }
}
