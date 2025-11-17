using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using Spamma.App;
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
}

public class TestWebApplicationFactory : WebApplicationFactory<Spamma.App.Infrastructure.Middleware.SetupModeMiddleware>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Directory.GetCurrentDirectory());
    }
}