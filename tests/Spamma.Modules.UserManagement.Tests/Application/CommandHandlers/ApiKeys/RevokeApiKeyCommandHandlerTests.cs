using BluQube.Commands;
using BluQube.Constants;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.ApiKey;
using Spamma.Modules.UserManagement.Application.CommandHandlers.ApiKeys;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;
using Spamma.Modules.UserManagement.Client.Contracts;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Tests.Fixtures;
using ApiKeyAggregate = Spamma.Modules.UserManagement.Domain.ApiKeys.ApiKey;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.ApiKeys;

public class RevokeApiKeyCommandHandlerTests
{
    private readonly Mock<IApiKeyRepository> _apiKeyRepositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<RevokeApiKeyCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly RevokeApiKeyCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public RevokeApiKeyCommandHandlerTests()
    {
        this._apiKeyRepositoryMock = new Mock<IApiKeyRepository>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        this._httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        this._loggerMock = new Mock<ILogger<RevokeApiKeyCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<RevokeApiKeyCommand>>();

        this._handler = new RevokeApiKeyCommandHandler(
            this._apiKeyRepositoryMock.Object,
            this._eventPublisherMock.Object,
            this._httpContextAccessorMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenApiKeyRevocationSucceeds_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var command = new RevokeApiKeyCommand(apiKeyId);

        // Create an active API key for the user
        var apiKey = ApiKeyAggregate.Create(
            apiKeyId,
            userId,
            "Test API Key",
            "hashedkey",
            "salt",
            this._timeProvider.GetUtcNow().DateTime).Value;

        // Mock authenticated user
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(x => x.IsAuthenticated).Returns(true);

        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Identity).Returns(identityMock.Object);
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        this._apiKeyRepositoryMock
            .Setup(x => x.GetByIdAsync(apiKeyId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(apiKey));

        this._apiKeyRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<ApiKeyRevokedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Succeeded);

        this._apiKeyRepositoryMock.Verify(
            x => x.GetByIdAsync(apiKeyId, CancellationToken.None),
            Times.Once);

        this._apiKeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<ApiKeyAggregate>(ak =>
                    ak.Id == apiKeyId &&
                    ak.IsRevoked &&
                    ak.RevokedAt == this._fixedUtcNow),
                CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<ApiKeyRevokedIntegrationEvent>(e =>
                    e.ApiKeyId == apiKeyId &&
                    e.UserId == userId &&
                    e.RevokedAt == this._fixedUtcNow),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsAuthenticationError()
    {
        // Arrange
        var apiKeyId = Guid.NewGuid();
        var command = new RevokeApiKeyCommand(apiKeyId);

        // Mock unauthenticated user
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData.Code.Should().Be(UserManagementErrorCodes.InvalidAuthenticationAttempt);

        this._apiKeyRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenApiKeyNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var command = new RevokeApiKeyCommand(apiKeyId);

        // Mock authenticated user
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(x => x.IsAuthenticated).Returns(true);

        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Identity).Returns(identityMock.Object);
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        this._apiKeyRepositoryMock
            .Setup(x => x.GetByIdAsync(apiKeyId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe<ApiKeyAggregate>.Nothing);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData.Code.Should().Be(UserManagementErrorCodes.ApiKeyNotFound);

        this._apiKeyRepositoryMock.Verify(
            x => x.GetByIdAsync(apiKeyId, CancellationToken.None),
            Times.Once);

        this._apiKeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenApiKeyBelongsToDifferentUser_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var command = new RevokeApiKeyCommand(apiKeyId);

        // Create an API key belonging to a different user
        var apiKey = ApiKeyAggregate.Create(
            apiKeyId,
            differentUserId,
            "Test API Key",
            "hashedkey",
            "salt",
            this._timeProvider.GetUtcNow().DateTime).Value;

        // Mock authenticated user
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(x => x.IsAuthenticated).Returns(true);

        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Identity).Returns(identityMock.Object);
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        this._apiKeyRepositoryMock
            .Setup(x => x.GetByIdAsync(apiKeyId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(apiKey));

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData.Code.Should().Be(UserManagementErrorCodes.ApiKeyNotFound);

        this._apiKeyRepositoryMock.Verify(
            x => x.GetByIdAsync(apiKeyId, CancellationToken.None),
            Times.Once);

        this._apiKeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenApiKeyAlreadyRevoked_ReturnsAlreadyRevokedError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var command = new RevokeApiKeyCommand(apiKeyId);

        // Create an already revoked API key
        var apiKey = ApiKeyAggregate.Create(
            apiKeyId,
            userId,
            "Test API Key",
            "hashedkey",
            "salt",
            this._timeProvider.GetUtcNow().DateTime).Value;

        // Revoke it
        apiKey.Revoke(this._timeProvider.GetUtcNow().DateTime);

        // Mock authenticated user
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(x => x.IsAuthenticated).Returns(true);

        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Identity).Returns(identityMock.Object);
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        this._apiKeyRepositoryMock
            .Setup(x => x.GetByIdAsync(apiKeyId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(apiKey));

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData.Code.Should().Be(UserManagementErrorCodes.ApiKeyAlreadyRevoked);

        this._apiKeyRepositoryMock.Verify(
            x => x.GetByIdAsync(apiKeyId, CancellationToken.None),
            Times.Once);

        this._apiKeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var command = new RevokeApiKeyCommand(apiKeyId);

        // Create an active API key for the user
        var apiKey = ApiKeyAggregate.Create(
            apiKeyId,
            userId,
            "Test API Key",
            "hashedkey",
            "salt",
            this._timeProvider.GetUtcNow().DateTime).Value;

        // Mock authenticated user
        var identityMock = new Mock<System.Security.Principal.IIdentity>();
        identityMock.Setup(x => x.IsAuthenticated).Returns(true);

        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Identity).Returns(identityMock.Object);
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        this._apiKeyRepositoryMock
            .Setup(x => x.GetByIdAsync(apiKeyId, CancellationToken.None))
            .ReturnsAsync(MaybeMonad.Maybe.From(apiKey));

        this._apiKeyRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Fail());

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Failed);
        result.ErrorData.Should().NotBeNull();
        result.ErrorData.Code.Should().Be(CommonErrorCodes.SavingChangesFailed);

        this._apiKeyRepositoryMock.Verify(
            x => x.GetByIdAsync(apiKeyId, CancellationToken.None),
            Times.Once);

        this._apiKeyRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<ApiKeyRevokedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
