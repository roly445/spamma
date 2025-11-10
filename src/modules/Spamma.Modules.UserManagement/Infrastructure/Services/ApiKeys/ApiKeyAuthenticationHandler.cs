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
        if (!this.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var isValid = await this.apiKeyValidationService.ValidateApiKeyAsync(apiKey);
        if (!isValid)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

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