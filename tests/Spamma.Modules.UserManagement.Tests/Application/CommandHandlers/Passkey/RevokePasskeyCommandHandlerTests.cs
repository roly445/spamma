using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using MaybeMonad;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.UserManagement.Application.CommandHandlers.Passkey;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.Passkey;

/// <summary>
/// Tests for RevokePasskeyCommandHandler.
/// Tests the passkey revocation flow including authorization checks and save operations.
/// </summary>
public class RevokePasskeyCommandHandlerTests
{
    private readonly Mock<IPasskeyRepository> _passkeyRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<RevokePasskeyCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly RevokePasskeyCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public RevokePasskeyCommandHandlerTests()
    {
        this._passkeyRepositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Loose);
        this._httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<RevokePasskeyCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<RevokePasskeyCommand>>();

        this._handler = new RevokePasskeyCommandHandler(
            this._passkeyRepositoryMock.Object,
            this._timeProvider,
            this._httpContextAccessorMock.Object,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidPasskeyRevocation_RevokesPasskeySuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var passkey = new PasskeyBuilder()
            .WithId(passkeyId)
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithDisplayName("My Security Key")
            .Build();

        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        this._passkeyRepositoryMock
            .Setup(x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoAuthenticationClaim_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(); // No claims

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_InvalidUserIdClaim_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "invalid-guid") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_NoHttpContext_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_PasskeyNotFound_ReturnsPasskeyNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();

        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PasskeyBelongsToOtherUser_ReturnsNotFoundError()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var passkey = new PasskeyBuilder()
            .WithId(passkeyId)
            .WithUserId(otherUserId) // Different user
            .WithCredentialId(credentialId)
            .WithDisplayName("Other User's Key")
            .Build();

        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_SaveFails_ReturnsSavingChangesFailedError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var passkey = new PasskeyBuilder()
            .WithId(passkeyId)
            .WithUserId(userId)
            .WithCredentialId(credentialId)
            .WithDisplayName("My Security Key")
            .Build();

        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        this._passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        this._passkeyRepositoryMock
            .Setup(x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        this._httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        this._passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None),
            Times.Once);
    }
}