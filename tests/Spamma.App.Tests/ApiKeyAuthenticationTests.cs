using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Spamma.App;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Spamma.App.Tests;

public class TestStartup
{
}

public class ApiKeyAuthenticationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ApiKeyAuthenticationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyApiKeys_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("api/user-management/api-keys/my");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyApiKeys_WithInvalidApiKeyHeader_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        client.DefaultRequestHeaders.Add("X-API-Key", "invalid-api-key");
        var response = await client.GetAsync("api/user-management/api-keys/my");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyApiKeys_WithInvalidApiKeyQueryParameter_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("api/user-management/api-keys/my?api_key=invalid-api-key");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmailContent_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var emailId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"email-inbox/get-email-mime-message-by-id?emailId={emailId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmailContent_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var emailId = Guid.NewGuid();

        // Act
        client.DefaultRequestHeaders.Add("X-API-Key", "invalid-api-key");
        var response = await client.GetAsync($"email-inbox/get-email-mime-message-by-id?emailId={emailId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmailMimeContent_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var emailId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"api/email-inbox/emails/{emailId}/mime-content");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmailMimeContent_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var emailId = Guid.NewGuid();

        // Act
        client.DefaultRequestHeaders.Add("X-API-Key", "invalid-api-key");
        var response = await client.GetAsync($"api/email-inbox/emails/{emailId}/mime-content");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Spamma.App.Infrastructure.Middleware.SetupModeMiddleware>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("spamma_app_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.ConfigureTestServices(services =>
            services.AddDataProtection().PersistKeysToFileSystem(
                new DirectoryInfo(Path.Combine(Path.GetTempPath(), "spamma-app-tests-keys"))));
    }
}
