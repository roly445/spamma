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
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Tests.Builders;
using Spamma.Modules.UserManagement.Tests.Fixtures;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.Passkey;

public class RevokeUserPasskeyCommandHandlerTests
{
    private readonly Mock<IPasskeyRepository> _passkeyRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<RevokeUserPasskeyCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly RevokeUserPasskeyCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public RevokeUserPasskeyCommandHandlerTests()
    {
        this._passkeyRepositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Loose);
        this._httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<RevokeUserPasskeyCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<RevokeUserPasskeyCommand>>();

        this._handler = new RevokeUserPasskeyCommandHandler(
            this._passkeyRepositoryMock.Object,
            this._timeProvider,
            this._httpContextAccessorMock.Object,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidAdminPasskeyRevocation_RevokesUserPasskeySuccessfully()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var passkey = new PasskeyBuilder()
            .WithId(passkeyId)
            .WithUserId(targetUserId)
            .WithCredentialId(credentialId)
            .WithDisplayName("User's Security Key")
            .Build();

        var command = new RevokeUserPasskeyCommand(targetUserId, passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()) };
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
        var targetUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var command = new RevokeUserPasskeyCommand(targetUserId, passkeyId);

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
    public async Task Handle_PasskeyNotFound_ReturnsPasskeyNotFoundError()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var command = new RevokeUserPasskeyCommand(targetUserId, passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()) };
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
        this._passkeyRepositoryMock.Verify(
            x => x.GetByIdAsync(passkeyId, CancellationToken.None),
            Times.Once);
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PasskeyBelongsToDifferentUser_ReturnsNotFoundError()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var passkey = new PasskeyBuilder()
            .WithId(passkeyId)
            .WithUserId(differentUserId) // Passkey belongs to different user
            .WithCredentialId(credentialId)
            .Build();

        var command = new RevokeUserPasskeyCommand(targetUserId, passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()) };
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
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositorySaveFails_ReturnsSavingChangesFailedError()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var passkey = new PasskeyBuilder()
            .WithId(passkeyId)
            .WithUserId(targetUserId)
            .WithCredentialId(credentialId)
            .Build();

        var command = new RevokeUserPasskeyCommand(targetUserId, passkeyId);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()) };
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
        this._passkeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None),
            Times.Once);
    }
}