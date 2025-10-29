using Microsoft.Extensions.Logging;
using Moq;
using Spamma.App.Infrastructure.Services;
using Xunit;

namespace Spamma.App.Tests.Infrastructure.Services;

/// <summary>
/// Integration tests for <see cref="AcmeChallengeServer"/>.
/// </summary>
public class AcmeChallengeServerTests
{
    /// <summary>
    /// Test: Register challenge stores token and key authentication.
    /// </summary>
    [Fact]
    public async Task RegisterChallengeAsync_ValidTokenAndKeyAuth_StoresSuccessfully()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token = "test-token-123";
        var keyAuth = "test-key-auth-456";

        // Act
        await server.RegisterChallengeAsync(token, keyAuth, CancellationToken.None);
        var storedKeyAuth = server.GetChallenge(token);

        // Verify
        Assert.NotNull(storedKeyAuth);
        Assert.Equal(keyAuth, storedKeyAuth);
    }

    /// <summary>
    /// Test: Get challenge returns stored key authentication.
    /// </summary>
    [Fact]
    public async Task GetChallenge_ExistingToken_ReturnsKeyAuth()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token = "existing-token";
        var keyAuth = "existing-key-auth";
        await server.RegisterChallengeAsync(token, keyAuth, CancellationToken.None);

        // Act
        var retrieved = server.GetChallenge(token);

        // Verify
        Assert.Equal(keyAuth, retrieved);
    }

    /// <summary>
    /// Test: Get challenge returns null for non-existent token.
    /// </summary>
    [Fact]
    public void GetChallenge_NonExistentToken_ReturnsNull()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token = "non-existent-token";

        // Act
        var retrieved = server.GetChallenge(token);

        // Verify
        Assert.Null(retrieved);
    }

    /// <summary>
    /// Test: Clear challenges removes all stored tokens.
    /// </summary>
    [Fact]
    public async Task ClearChallengesAsync_MultipleTokens_RemovesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token1 = "token-1";
        var token2 = "token-2";
        await server.RegisterChallengeAsync(token1, "key-auth-1", CancellationToken.None);
        await server.RegisterChallengeAsync(token2, "key-auth-2", CancellationToken.None);

        // Act
        await server.ClearChallengesAsync(CancellationToken.None);
        var retrieved1 = server.GetChallenge(token1);
        var retrieved2 = server.GetChallenge(token2);

        // Verify
        Assert.Null(retrieved1);
        Assert.Null(retrieved2);
    }

    /// <summary>
    /// Test: Multiple tokens can be registered independently.
    /// </summary>
    [Fact]
    public async Task RegisterChallengeAsync_MultipleTokens_StoresEachIndependently()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token1 = "token-1";
        var keyAuth1 = "key-auth-1";
        var token2 = "token-2";
        var keyAuth2 = "key-auth-2";

        // Act
        await server.RegisterChallengeAsync(token1, keyAuth1, CancellationToken.None);
        await server.RegisterChallengeAsync(token2, keyAuth2, CancellationToken.None);

        var retrieved1 = server.GetChallenge(token1);
        var retrieved2 = server.GetChallenge(token2);

        // Verify
        Assert.Equal(keyAuth1, retrieved1);
        Assert.Equal(keyAuth2, retrieved2);
    }

    /// <summary>
    /// Test: Registering same token twice updates the value.
    /// </summary>
    [Fact]
    public async Task RegisterChallengeAsync_DuplicateToken_UpdatesValue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token = "token-123";
        var keyAuth1 = "initial-key-auth";
        var keyAuth2 = "updated-key-auth";

        // Act
        await server.RegisterChallengeAsync(token, keyAuth1, CancellationToken.None);
        await server.RegisterChallengeAsync(token, keyAuth2, CancellationToken.None);
        var retrieved = server.GetChallenge(token);

        // Verify
        Assert.Equal(keyAuth2, retrieved);
    }

    /// <summary>
    /// Test: Logger is invoked when challenge is registered.
    /// </summary>
    [Fact]
    public async Task RegisterChallengeAsync_LogsInformation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token = "test-token";
        var keyAuth = "test-key-auth";

        // Act
        await server.RegisterChallengeAsync(token, keyAuth, CancellationToken.None);

        // Verify
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registered HTTP-01 challenge")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Logger is invoked when challenges are cleared.
    /// </summary>
    [Fact]
    public async Task ClearChallengesAsync_LogsInformation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var token = "test-token";
        await server.RegisterChallengeAsync(token, "test-key-auth", CancellationToken.None);

        // Act
        await server.ClearChallengesAsync(CancellationToken.None);

        // Verify
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleared")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Thread-safety with concurrent registrations.
    /// </summary>
    [Fact]
    public async Task RegisterChallengeAsync_ConcurrentRegistrations_AllSucceed()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeServer>>();
        var server = new AcmeChallengeServer(loggerMock.Object);

        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var token = $"token-{i}";
            var keyAuth = $"key-auth-{i}";
            tasks.Add(server.RegisterChallengeAsync(token, keyAuth, CancellationToken.None));
        }

        // Act
        await Task.WhenAll(tasks);

        // Verify - All registrations should have succeeded
        for (int i = 0; i < 10; i++)
        {
            var retrieved = server.GetChallenge($"token-{i}");
            Assert.NotNull(retrieved);
            Assert.Equal($"key-auth-{i}", retrieved);
        }
    }
}
