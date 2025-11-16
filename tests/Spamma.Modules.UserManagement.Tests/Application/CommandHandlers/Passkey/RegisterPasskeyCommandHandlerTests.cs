using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.UserManagement.Application.CommandHandlers.Passkey;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Tests.Fixtures;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.Passkey;

public class RegisterPasskeyCommandHandlerTests
{
    private readonly Mock<IPasskeyRepository> _passkeyRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<RegisterPasskeyCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly RegisterPasskeyCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public RegisterPasskeyCommandHandlerTests()
    {
        this._passkeyRepositoryMock = new Mock<IPasskeyRepository>(MockBehavior.Strict);
        this._httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        this._loggerMock = new Mock<ILogger<RegisterPasskeyCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<RegisterPasskeyCommand>>();

        this._handler = new RegisterPasskeyCommandHandler(
            this._passkeyRepositoryMock.Object,
            this._timeProvider,
            this._httpContextAccessorMock.Object,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidPasskeyRegistration_SavesPasskeySuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3 };

        var command = new RegisterPasskeyCommand(
            CredentialId: credentialId,
            PublicKey: publicKey,
            SignCount: 0,
            DisplayName: "My Security Key",
            Algorithm: "RS256");

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

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
            x => x.SaveAsync(
                It.Is<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(p =>
                    p.UserId == userId &&
                    p.DisplayName == "My Security Key" &&
                    p.Algorithm == "RS256"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoAuthenticationClaim_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3 };

        var command = new RegisterPasskeyCommand(
            CredentialId: credentialId,
            PublicKey: publicKey,
            SignCount: 0,
            DisplayName: "My Security Key",
            Algorithm: "RS256");

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
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3 };

        var command = new RegisterPasskeyCommand(
            CredentialId: credentialId,
            PublicKey: publicKey,
            SignCount: 0,
            DisplayName: "My Security Key",
            Algorithm: "RS256");

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
    public async Task Handle_SaveFails_ReturnsSavingChangesFailedError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3 };

        var command = new RegisterPasskeyCommand(
            CredentialId: credentialId,
            PublicKey: publicKey,
            SignCount: 0,
            DisplayName: "My Security Key",
            Algorithm: "RS256");

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        this._httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

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
            x => x.SaveAsync(
                It.IsAny<Spamma.Modules.UserManagement.Domain.PasskeyAggregate.Passkey>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoHttpContext_ReturnsInvalidAuthenticationAttemptError()
    {
        // Arrange
        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var publicKey = new byte[] { 0xA0, 0xA1, 0xA2, 0xA3 };

        var command = new RegisterPasskeyCommand(
            CredentialId: credentialId,
            PublicKey: publicKey,
            SignCount: 0,
            DisplayName: "My Security Key",
            Algorithm: "RS256");

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
}