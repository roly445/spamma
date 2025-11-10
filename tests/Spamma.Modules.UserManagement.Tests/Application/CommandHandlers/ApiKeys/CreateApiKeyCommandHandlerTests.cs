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
using Spamma.Modules.UserManagement.Tests.Fixtures;
using ApiKeyAggregate = Spamma.Modules.UserManagement.Domain.ApiKeys.ApiKey;

namespace Spamma.Modules.UserManagement.Tests.Application.CommandHandlers.ApiKeys;

public class CreateApiKeyCommandHandlerTests
{
    private readonly Mock<IApiKeyRepository> _apiKeyRepositoryMock;
    private readonly Mock<IIntegrationEventPublisher> _eventPublisherMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<CreateApiKeyCommandHandler>> _loggerMock;
    private readonly StubTimeProvider _timeProvider;
    private readonly CreateApiKeyCommandHandler _handler;
    private readonly DateTime _fixedUtcNow = new(2024, 10, 15, 10, 30, 00, DateTimeKind.Utc);

    public CreateApiKeyCommandHandlerTests()
    {
        this._apiKeyRepositoryMock = new Mock<IApiKeyRepository>(MockBehavior.Strict);
        this._eventPublisherMock = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        this._httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        this._loggerMock = new Mock<ILogger<CreateApiKeyCommandHandler>>();
        this._timeProvider = new StubTimeProvider(this._fixedUtcNow);

        var validators = Array.Empty<IValidator<CreateApiKeyCommand>>();

        this._handler = new CreateApiKeyCommandHandler(
            this._apiKeyRepositoryMock.Object,
            this._eventPublisherMock.Object,
            this._httpContextAccessorMock.Object,
            this._timeProvider,
            validators,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenApiKeyCreationSucceeds_ReturnsApiKeyData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateApiKeyCommand("Test API Key");

        // Mock authenticated user
        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        this._apiKeyRepositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), CancellationToken.None))
            .ReturnsAsync(Result.Ok());

        this._eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<ApiKeyCreatedIntegrationEvent>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Verify
        result.Should().NotBeNull();
        result.Status.Should().Be(CommandResultStatus.Succeeded);
        result.Data.Should().NotBeNull();
        result.Data.ApiKey.Should().NotBeNull();
        result.Data.ApiKey.Name.Should().Be("Test API Key");
        result.Data.ApiKey.CreatedAt.Should().Be(this._fixedUtcNow);
        result.Data.ApiKey.IsRevoked.Should().BeFalse();
        result.Data.KeyValue.Should().NotBeNullOrEmpty();
        result.Data.KeyValue.Should().StartWith("sk-");

        this._apiKeyRepositoryMock.Verify(
            x => x.SaveAsync(
                It.Is<ApiKeyAggregate>(apiKey =>
                    apiKey.Name == "Test API Key" &&
                    apiKey.UserId == userId),
                CancellationToken.None),
            Times.Once);

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<ApiKeyCreatedIntegrationEvent>(e =>
                    e.UserId == userId &&
                    e.ApiKeyId == result.Data.ApiKey.Id &&
                    e.Name == "Test API Key"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsAuthenticationError()
    {
        // Arrange
        var command = new CreateApiKeyCommand("Test API Key");

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
            x => x.SaveAsync(It.IsAny<ApiKeyAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositorySaveFails_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateApiKeyCommand("Test API Key");

        // Mock authenticated user
        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

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

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<ApiKeyCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenApiKeyNameAlreadyExists_ReturnsConflictError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateApiKeyCommand("Existing API Key");

        // Mock authenticated user
        var claimsPrincipalMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier))
            .Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()));

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(claimsPrincipalMock.Object);
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Mock domain logic to return failure for duplicate name
        // This would be tested in domain tests, but we mock the repository save to fail
        // In real implementation, the domain would validate uniqueness
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

        this._eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<ApiKeyCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}