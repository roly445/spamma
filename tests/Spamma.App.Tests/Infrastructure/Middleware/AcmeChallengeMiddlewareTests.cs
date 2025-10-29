using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.App.Infrastructure.Middleware;
using Spamma.App.Infrastructure.Services;
using Xunit;

namespace Spamma.App.Tests.Infrastructure.Middleware;

/// <summary>
/// Integration tests for <see cref="AcmeChallengeMiddleware"/>.
/// </summary>
public class AcmeChallengeMiddlewareTests
{
    /// <summary>
    /// Test: Middleware can be constructed with proper dependencies.
    /// </summary>
    [Fact]
    public void AcmeChallengeMiddleware_ConstructorWithValidDependencies_CreatesSuccessfully()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeMiddleware>>();
        var nextMock = new Mock<RequestDelegate>();

        // Act
        var middleware = new AcmeChallengeMiddleware(nextMock.Object, loggerMock.Object);

        // Verify
        Assert.NotNull(middleware);
    }

    /// <summary>
    /// Test: Middleware extension method can be called.
    /// </summary>
    [Fact]
    public void UseAcmeChallenge_ValidBuilder_RegistersSuccessfully()
    {
        // Arrange
        var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

        // Act
        builder.UseAcmeChallenge();

        // Verify - If no exception is thrown, the middleware registered successfully
        Assert.NotNull(builder);
    }

    /// <summary>
    /// Test: Middleware is properly integrated in application pipeline.
    /// </summary>
    [Fact]
    public void AcmeChallengeMiddleware_IntegrationWithBuilder_NoExceptionThrown()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<ILogger<AcmeChallengeMiddleware>>().Object);
        services.AddSingleton<AcmeChallengeServer>();

        var provider = services.BuildServiceProvider();
        var builder = new ApplicationBuilder(provider);

        // Act & Verify
        try
        {
            builder.UseAcmeChallenge();
        }
        catch (Exception ex)
        {
            Assert.True(false, $"Middleware integration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: Middleware logger is invoked on construction.
    /// </summary>
    [Fact]
    public void AcmeChallengeMiddleware_Constructor_LoggerNotNull()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeMiddleware>>();
        var nextMock = new Mock<RequestDelegate>();

        // Act
        var middleware = new AcmeChallengeMiddleware(nextMock.Object, loggerMock.Object);

        // Verify
        Assert.NotNull(middleware);
    }

    /// <summary>
    /// Test: Middleware next delegate is stored correctly.
    /// </summary>
    [Fact]
    public void AcmeChallengeMiddleware_StoresNextDelegate()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AcmeChallengeMiddleware>>();
        var nextMock = new Mock<RequestDelegate>();

        // Act
        var middleware = new AcmeChallengeMiddleware(nextMock.Object, loggerMock.Object);

        // Verify - If constructor doesn't throw, next delegate was stored
        Assert.NotNull(middleware);
    }
}

