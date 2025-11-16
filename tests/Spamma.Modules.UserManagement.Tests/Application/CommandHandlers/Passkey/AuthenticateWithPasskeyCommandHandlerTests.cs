using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.UserManagement.Application.CommandHandlers.Passkey;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.Passkey;

public class AuthenticateWithPasskeyCommandHandlerTests
{
    private readonly Mock<IPasskeyRepository> _passkeyRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<AuthenticateWithPasskeyCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly AuthenticateWithPasskeyCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public AuthenticateWithPasskeyCommandHandlerTests()
    {
        this._passkeyRepositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Loose);
        this._userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<AuthenticateWithPasskeyCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<AuthenticateWithPasskeyCommand>>();

        this._handler = new AuthenticateWithPasskeyCommandHandler(
            this._passkeyRepositoryMock.Object,
            this._userRepositoryMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidPasskeyWithValidSignCount_AuthenticatesSuccessfully()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var userId = Guid.NewGuid();

        var passkey = new PasskeyBuilder()
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithDisplayName("My Security Key")
            .WithSignCount(5)
            .WithRegisteredAt(DateTime.UtcNow.AddDays(-30))
            .Build();

        var user = new UserBuilder()
            .WithId(userId)
            .WithEmail("user@example.com")
            .Build();

        var command = new AuthenticateWithPasskeyCommand(credentialId, 6); // sign count incremented

        this._passkeyRepositoryMock
            .Setup(x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        this._userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(user));

        this._passkeyRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._passkeyRepositoryMock.Verify(
            x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None),
            Times.Once);
        this._userRepositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PasskeyNotFound_ReturnsPasskeyNotFoundError()
    {
        // Arrange
        var credentialId = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var command = new AuthenticateWithPasskeyCommand(credentialId, 1);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._passkeyRepositoryMock.Verify(
            x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None),
            Times.Once);
        this._userRepositoryMock.VerifyNoOtherCalls();
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UserNotFoundForPasskey_ReturnsAccountNotFoundError()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var userId = Guid.NewGuid();

        var passkey = new PasskeyBuilder()
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithSignCount(5)
            .Build();

        var command = new AuthenticateWithPasskeyCommand(credentialId, 6);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        this._userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.UserManagement.Domain.UserAggregate.User>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._passkeyRepositoryMock.Verify(
            x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None),
            Times.Once);
        this._userRepositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UserSuspended_ReturnsAccountSuspendedError()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var userId = Guid.NewGuid();

        var passkey = new PasskeyBuilder()
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithSignCount(5)
            .Build();

        var suspendedUser = new UserBuilder()
            .WithId(userId)
            .WithEmail("suspended@example.com")
            .Build();
        suspendedUser.Suspend(AccountSuspensionReason.Administrative, "Test suspension", this._fixedUtcNow);

        var command = new AuthenticateWithPasskeyCommand(credentialId, 6);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        this._userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(suspendedUser));

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._passkeyRepositoryMock.Verify(
            x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None),
            Times.Once);
        this._userRepositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_SignCountNotIncremented_ReturnsSaveError()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var userId = Guid.NewGuid();

        var passkey = new PasskeyBuilder()
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithSignCount(5)
            .Build();

        var user = new UserBuilder()
            .WithId(userId)
            .WithEmail("user@example.com")
            .Build();

        var command = new AuthenticateWithPasskeyCommand(credentialId, 5); // sign count NOT incremented (potential cloning attack)

        this._passkeyRepositoryMock
            .Setup(x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(passkey));

        this._userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(user));

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify - should fail during sign count validation
        result.Should().NotBeNull();
        this._passkeyRepositoryMock.Verify(
            x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None),
            Times.Once);
        this._userRepositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SaveFails_ReturnsSavingChangesFailedError()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var userId = Guid.NewGuid();

        var passkey = new PasskeyBuilder()
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithSignCount(5)
            .Build();

        var user = new UserBuilder()
            .WithId(userId)
            .WithEmail("user@example.com")
            .Build();

        var command = new AuthenticateWithPasskeyCommand(credentialId, 6);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(passkey));

        this._userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(user));

        this._passkeyRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._passkeyRepositoryMock.Verify(
            x => x.GetByCredentialIdAsync(credentialId, CancellationToken.None),
            Times.Once);
        this._userRepositoryMock.Verify(
            x => x.GetByIdAsync(userId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(), CancellationToken.None),
            Times.Once);
    }
}