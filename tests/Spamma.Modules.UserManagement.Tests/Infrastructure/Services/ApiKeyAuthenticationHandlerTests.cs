using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;
using Xunit;

namespace Spamma.Modules.UserManagement.Tests.Infrastructure.Services;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> _optionsMonitorMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<UrlEncoder> _urlEncoderMock;
    private readonly Mock<IApiKeyValidationService> _apiKeyValidationServiceMock;
    private readonly ApiKeyAuthenticationHandler _handler;
    private readonly DefaultHttpContext _httpContext;

    public ApiKeyAuthenticationHandlerTests()
    {
        this._optionsMonitorMock = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        this._loggerFactoryMock = new Mock<ILoggerFactory>();
        this._urlEncoderMock = new Mock<UrlEncoder>();
        this._apiKeyValidationServiceMock = new Mock<IApiKeyValidationService>();

        // Setup default options
        var options = new ApiKeyAuthenticationOptions
        {
            HeaderName = "X-API-Key",
            QueryParameterName = "api_key",
            AllowQueryParameter = true,
        };
        this._optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        // Setup logger
        var loggerMock = new Mock<ILogger<ApiKeyAuthenticationHandler>>();
        this._loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

        this._handler = new ApiKeyAuthenticationHandler(
            this._optionsMonitorMock.Object,
            this._loggerFactoryMock.Object,
            this._urlEncoderMock.Object,
            this._apiKeyValidationServiceMock.Object);

        this._httpContext = new DefaultHttpContext();
        this._handler.InitializeAsync(new AuthenticationScheme("ApiKey", null, typeof(ApiKeyAuthenticationHandler)), this._httpContext);

        // Set the Context property using reflection since it's protected
        var contextProperty = typeof(AuthenticationHandler<ApiKeyAuthenticationOptions>).GetProperty("Context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        contextProperty?.SetValue(this._handler, this._httpContext);

        // Also set the Request property directly
        var requestProperty = typeof(AuthenticationHandler<ApiKeyAuthenticationOptions>).GetProperty("Request", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        requestProperty?.SetValue(this._handler, this._httpContext.Request);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_NoApiKey_ReturnsNoResult()
    {
        // Arrange
        this._httpContext.Request.Headers.Clear();
        this._httpContext.Request.QueryString = QueryString.Empty;

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.Equal(AuthenticateResult.NoResult(), result);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_EmptyApiKeyHeader_ReturnsNoResult()
    {
        // Arrange
        this._httpContext.Request.Headers["X-API-Key"] = string.Empty;

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.Equal(AuthenticateResult.NoResult(), result);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhitespaceApiKeyHeader_ReturnsNoResult()
    {
        // Arrange
        this._httpContext.Request.Headers["X-API-Key"] = "   ";

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.Equal(AuthenticateResult.NoResult(), result);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidApiKey_ReturnsFail()
    {
        // Arrange
        var apiKey = "invalid-api-key";
        this._httpContext.Request.Headers["X-API-Key"] = apiKey;
        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(apiKey, default)).ReturnsAsync(false);

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.True(result.Failure != null);
        Assert.Contains("Invalid API key", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidApiKeyInHeader_ReturnsSuccess()
    {
        // Arrange
        var apiKey = "valid-api-key";
        this._httpContext.Request.Headers["X-API-Key"] = apiKey;
        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(apiKey, default)).ReturnsAsync(true);

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("api-user", result.Principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("ApiKey", result.Principal.FindFirstValue(ClaimTypes.AuthenticationMethod));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidApiKeyInQueryParameter_ReturnsSuccess()
    {
        // Arrange
        var apiKey = "valid-api-key";
        this._httpContext.Request.QueryString = new QueryString($"?api_key={apiKey}");
        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(apiKey, default)).ReturnsAsync(true);

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("api-user", result.Principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("ApiKey", result.Principal.FindFirstValue(ClaimTypes.AuthenticationMethod));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_HeaderTakesPrecedenceOverQueryParameter()
    {
        // Arrange
        var headerApiKey = "header-api-key";
        var queryApiKey = "query-api-key";
        this._httpContext.Request.Headers["X-API-Key"] = headerApiKey;
        this._httpContext.Request.QueryString = new QueryString($"?api_key={queryApiKey}");

        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(headerApiKey, default)).ReturnsAsync(true);
        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(queryApiKey, default)).ReturnsAsync(false);

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        this._apiKeyValidationServiceMock.Verify(x => x.ValidateApiKeyAsync(headerApiKey, default), Times.Once);
        this._apiKeyValidationServiceMock.Verify(x => x.ValidateApiKeyAsync(queryApiKey, default), Times.Never);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_QueryParameterDisabled_IgnoresQueryParameter()
    {
        // Arrange
        var options = new ApiKeyAuthenticationOptions
        {
            HeaderName = "X-API-Key",
            QueryParameterName = "api_key",
            AllowQueryParameter = false,
        };
        this._optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var apiKey = "valid-api-key";
        this._httpContext.Request.QueryString = new QueryString($"?api_key={apiKey}");

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.Equal(AuthenticateResult.NoResult(), result);
        this._apiKeyValidationServiceMock.Verify(x => x.ValidateApiKeyAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_CustomHeaderName_Works()
    {
        // Arrange
        var options = new ApiKeyAuthenticationOptions
        {
            HeaderName = "X-Custom-Key",
            QueryParameterName = "api_key",
            AllowQueryParameter = true,
        };
        this._optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var apiKey = "valid-api-key";
        this._httpContext.Request.Headers["X-Custom-Key"] = apiKey;
        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(apiKey, default)).ReturnsAsync(true);

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_CustomQueryParameterName_Works()
    {
        // Arrange
        var options = new ApiKeyAuthenticationOptions
        {
            HeaderName = "X-API-Key",
            QueryParameterName = "custom_key",
            AllowQueryParameter = true,
        };
        this._optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var apiKey = "valid-api-key";
        this._httpContext.Request.QueryString = new QueryString($"?custom_key={apiKey}");
        this._apiKeyValidationServiceMock.Setup(x => x.ValidateApiKeyAsync(apiKey, default)).ReturnsAsync(true);

        // Act
        var result = await this.CallHandleAuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
    }

    private async Task<AuthenticateResult> CallHandleAuthenticateAsync()
    {
        var method = typeof(ApiKeyAuthenticationHandler).GetMethod("HandleAuthenticateAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return await (Task<AuthenticateResult>)method!.Invoke(this._handler, null)!;
    }
}