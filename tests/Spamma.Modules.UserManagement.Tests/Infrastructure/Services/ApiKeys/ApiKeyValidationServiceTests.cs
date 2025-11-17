using System;
using System.Threading.Tasks;
using MaybeMonad;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spamma.Modules.UserManagement.Application.Repositories;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;
using Xunit;

namespace Spamma.Modules.UserManagement.Tests.Infrastructure.Services.ApiKeys;

public class ApiKeyValidationServiceTests
{
    [Fact]
    public async Task ValidateApiKeyAsync_FindsPlainKey_ReturnsTrue()
    {
        // Arrange - Use in-memory cache for integration testing
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        var apiKeyValue = "sk-abcdefgh123456";
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var keyHashPrefix = "prefix_hash";
        var keyHash = BCrypt.Net.BCrypt.HashPassword(apiKeyValue);

        var apiKeyAggregate = Spamma.Modules.UserManagement.Domain.ApiKeys.ApiKey.Create(
            apiKeyId,
            userId,
            "test",
            keyHashPrefix,
            keyHash,
            DateTime.UtcNow).Value;

        var repoMock = new Mock<IApiKeyRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.GetByPlainKeyAsync(apiKeyValue, default)).ReturnsAsync(Maybe.From(apiKeyAggregate));

        var service = new ApiKeyValidationService(repoMock.Object, cache);

        // Act
        var result = await service.ValidateApiKeyAsync(apiKeyValue);

        // Assert
        Assert.True(result);
        repoMock.Verify(r => r.GetByPlainKeyAsync(apiKeyValue, default), Times.Once);

        // Verify caching - second call should use cache and not query repository
        repoMock.Reset();
        var cachedResult = await service.ValidateApiKeyAsync(apiKeyValue);
        Assert.True(cachedResult);
        repoMock.Verify(r => r.GetByPlainKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_KeyNotFound_ReturnsFalseAndCaches()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        var apiKeyValue = "sk-nonexistent";

        var repoMock = new Mock<IApiKeyRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.GetByPlainKeyAsync(apiKeyValue, default)).ReturnsAsync(Maybe<ApiKey>.Nothing);

        var service = new ApiKeyValidationService(repoMock.Object, cache);

        // Act
        var result = await service.ValidateApiKeyAsync(apiKeyValue);

        // Assert
        Assert.False(result);
        repoMock.Verify(r => r.GetByPlainKeyAsync(apiKeyValue, default), Times.Once);

        // Verify negative result is cached - second call should not query repository
        repoMock.Reset();
        var cachedResult = await service.ValidateApiKeyAsync(apiKeyValue);
        Assert.False(cachedResult);
        repoMock.Verify(r => r.GetByPlainKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_RevokedKey_ReturnsFalseAndCaches()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        var apiKeyValue = "sk-revoked";
        var apiKeyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var keyHashPrefix = "prefix_hash";
        var keyHash = BCrypt.Net.BCrypt.HashPassword(apiKeyValue);

        var apiKeyAggregate = Spamma.Modules.UserManagement.Domain.ApiKeys.ApiKey.Create(
            apiKeyId,
            userId,
            "test",
            keyHashPrefix,
            keyHash,
            DateTime.UtcNow).Value;

        // Revoke the key
        apiKeyAggregate.Revoke(TimeProvider.System.GetUtcNow().DateTime);

        var repoMock = new Mock<IApiKeyRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.GetByPlainKeyAsync(apiKeyValue, default)).ReturnsAsync(Maybe.From(apiKeyAggregate));

        var service = new ApiKeyValidationService(repoMock.Object, cache);

        // Act
        var result = await service.ValidateApiKeyAsync(apiKeyValue);

        // Assert
        Assert.False(result);
        repoMock.Verify(r => r.GetByPlainKeyAsync(apiKeyValue, default), Times.Once);

        // Verify revoked result is cached
        repoMock.Reset();
        var cachedResult = await service.ValidateApiKeyAsync(apiKeyValue);
        Assert.False(cachedResult);
        repoMock.Verify(r => r.GetByPlainKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_EmptyOrWhitespace_ReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        var repoMock = new Mock<IApiKeyRepository>(MockBehavior.Strict);
        var service = new ApiKeyValidationService(repoMock.Object, cache);

        // Act & Assert
        Assert.False(await service.ValidateApiKeyAsync(string.Empty));
        Assert.False(await service.ValidateApiKeyAsync("   "));
        Assert.False(await service.ValidateApiKeyAsync(null!));

        // Verify repository was never called
        repoMock.Verify(r => r.GetByPlainKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
