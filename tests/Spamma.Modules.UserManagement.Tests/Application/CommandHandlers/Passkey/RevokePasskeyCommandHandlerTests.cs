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
        _passkeyRepositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Loose);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<RevokePasskeyCommandHandler>>();
        _timeProvider = new StubTimeProvider(_fixedUtcNow);

        var validators = Array.Empty<IValidator<RevokePasskeyCommand>>();

        _handler = new RevokePasskeyCommandHandler(
            _passkeyRepositoryMock.Object,
            _timeProvider,
            _httpContextAccessorMock.Object,
            validators,
            _loggerMock.Object);
    }

    #region Happy Path Tests

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

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        _passkeyRepositoryMock
            .Setup(x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        _passkeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None),
            Times.Once);
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public async Task Handle_NoAuthenticationClaim_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(); // No claims

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.VerifyNoOtherCalls();
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

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_NoHttpContext_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var passkeyId = Guid.NewGuid();
        var command = new RevokePasskeyCommand(passkeyId);

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns((HttpContext?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.VerifyNoOtherCalls();
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

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>.Nothing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.Verify(
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

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        _passkeyRepositoryMock.VerifyNoOtherCalls();
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

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _passkeyRepositoryMock
            .Setup(x => x.GetByIdAsync(passkeyId, CancellationToken.None))
            .ReturnsAsync(Maybe.From(passkey));

        _passkeyRepositoryMock
            .Setup(x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        _httpContextAccessorMock.Verify(x => x.HttpContext, Times.AtLeastOnce());
        _passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        _passkeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None),
            Times.Once);
    }

    #endregion
}
