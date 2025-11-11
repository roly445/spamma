using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidationService apiKeyValidationService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidationService apiKeyValidationService)
        : base(options, logger, encoder)
    {
        this.apiKeyValidationService = apiKeyValidationService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? apiKey = null;

        // Try header first
        if (this.Request.Headers.TryGetValue(this.Options.HeaderName, out var apiKeyHeaderValues))
        {
            apiKey = apiKeyHeaderValues.FirstOrDefault();
        }

        // If not found in header and query parameters are allowed, try query parameter
        if (string.IsNullOrWhiteSpace(apiKey) && this.Options.AllowQueryParameter &&
            this.Request.Query.TryGetValue(this.Options.QueryParameterName, out var apiKeyQueryValues))
        {
            apiKey = apiKeyQueryValues.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            this.Logger.LogDebug(
                "No API key found in header '{HeaderName}' or query parameter '{QueryParameterName}'",
                this.Options.HeaderName,
                this.Options.QueryParameterName);
            return AuthenticateResult.NoResult();
        }

        // Log authentication attempt (mask the key for security)
        var maskedKey = apiKey.Length > 8 ? $"{apiKey[..4]}****{apiKey[^4..]}" : "****";
        var authMethod = this.Request.Headers.ContainsKey(this.Options.HeaderName) ? "header" : "query parameter";
        this.Logger.LogInformation(
            "Attempting API key authentication for key: {MaskedApiKey} via {AuthMethod}",
            maskedKey,
            authMethod);

        var isValid = await this.apiKeyValidationService.ValidateApiKeyAsync(apiKey);
        if (!isValid)
        {
            this.Logger.LogWarning(
                "API key authentication failed for key: {MaskedApiKey} via {AuthMethod}",
                maskedKey,
                authMethod);
            return AuthenticateResult.Fail("Invalid API key");
        }

        this.Logger.LogInformation(
            "API key authentication successful for key: {MaskedApiKey} via {AuthMethod}",
            maskedKey,
            authMethod);

        // For now, create a minimal identity. In a real implementation,
        // you'd want to look up the user associated with the API key
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "api-user"),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
        };

        var identity = new ClaimsIdentity(claims, this.Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}