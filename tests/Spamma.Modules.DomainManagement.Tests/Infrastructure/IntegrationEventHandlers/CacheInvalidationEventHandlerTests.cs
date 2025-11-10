using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.Common.Caching;
using Spamma.Modules.Common.IntegrationEvents.DomainManagement;
using Spamma.Modules.DomainManagement.Infrastructure.IntegrationEventHandlers;

namespace Spamma.Modules.DomainManagement.Tests.Infrastructure.IntegrationEventHandlers;

public class CacheInvalidationEventHandlerTests
{
    private readonly Mock<ISubdomainCache> _subdomainCacheMock;
    private readonly Mock<IChaosAddressCache> _chaosAddressCacheMock;
    private readonly Mock<ILogger<CacheInvalidationEventHandler>> _loggerMock;
    private readonly CacheInvalidationEventHandler _handler;

    public CacheInvalidationEventHandlerTests()
    {
        _subdomainCacheMock = new Mock<ISubdomainCache>(MockBehavior.Strict);
        _chaosAddressCacheMock = new Mock<IChaosAddressCache>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<CacheInvalidationEventHandler>>();
        _handler = new CacheInvalidationEventHandler(
            _subdomainCacheMock.Object,
            _chaosAddressCacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task OnSubdomainStatusChanged_InvalidatesSubdomainCache()
    {
        // Arrange
        var ev = new SubdomainStatusChangedIntegrationEvent(
            SubdomainId: Guid.NewGuid(),
            DomainName: "example.spamma.io");

        _subdomainCacheMock
            .Setup(x => x.InvalidateAsync("example.spamma.io", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.OnSubdomainStatusChanged(ev);

        // Assert
        _subdomainCacheMock.Verify(x => x.InvalidateAsync("example.spamma.io", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnSubdomainStatusChanged_LogsInformation()
    {
        // Arrange
        var ev = new SubdomainStatusChangedIntegrationEvent(
            SubdomainId: Guid.NewGuid(),
            DomainName: "test.spamma.io");

        _subdomainCacheMock
            .Setup(x => x.InvalidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.OnSubdomainStatusChanged(ev);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test.spamma.io")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnSubdomainStatusChanged_WhenCacheInvalidationFails_LogsWarning()
    {
        // Arrange
        var ev = new SubdomainStatusChangedIntegrationEvent(
            SubdomainId: Guid.NewGuid(),
            DomainName: "failing.spamma.io");

        var exception = new InvalidOperationException("Cache error");
        _subdomainCacheMock
            .Setup(x => x.InvalidateAsync("failing.spamma.io", It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _handler.OnSubdomainStatusChanged(ev);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failing.spamma.io")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnChaosAddressUpdated_InvalidatesChaosAddressCache()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var ev = new ChaosAddressUpdatedIntegrationEvent(
            ChaosAddressId: Guid.NewGuid(),
            SubdomainId: subdomainId);

        _chaosAddressCacheMock
            .Setup(x => x.InvalidateBySubdomainAsync(subdomainId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.OnChaosAddressUpdated(ev);

        // Assert
        _chaosAddressCacheMock.Verify(x => x.InvalidateBySubdomainAsync(subdomainId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnChaosAddressUpdated_LogsInformation()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var ev = new ChaosAddressUpdatedIntegrationEvent(
            ChaosAddressId: Guid.NewGuid(),
            SubdomainId: subdomainId);

        _chaosAddressCacheMock
            .Setup(x => x.InvalidateBySubdomainAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.OnChaosAddressUpdated(ev);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(subdomainId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnChaosAddressUpdated_WhenCacheInvalidationFails_LogsWarning()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var ev = new ChaosAddressUpdatedIntegrationEvent(
            ChaosAddressId: Guid.NewGuid(),
            SubdomainId: subdomainId);

        var exception = new InvalidOperationException("Cache error");
        _chaosAddressCacheMock
            .Setup(x => x.InvalidateBySubdomainAsync(subdomainId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _handler.OnChaosAddressUpdated(ev);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(subdomainId.ToString())),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
