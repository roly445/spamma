using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.ApiKey;

namespace Spamma.Modules.UserManagement.Infrastructure.Services.ApiKeys;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyValidationService apiKeyValidationService,
    IIntegrationEventPublisher integrationEventPublisher,
    IApiKeyRateLimiter apiKeyRateLimiter)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
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

        // Check rate limits first
        var isWithinRateLimit = await apiKeyRateLimiter.IsWithinRateLimitAsync(apiKey);
        if (!isWithinRateLimit)
        {
            this.Logger.LogWarning(
                "API key authentication rate limited for key: {MaskedApiKey} via {AuthMethod}",
                maskedKey,
                authMethod);

            // Publish audit event for rate limited authentication
            await this.PublishAuthenticationAuditEventAsync(maskedKey, authMethod, false, "Rate limit exceeded");

            return AuthenticateResult.Fail("Rate limit exceeded");
        }

        var isValid = await apiKeyValidationService.ValidateApiKeyAsync(apiKey);
        if (!isValid)
        {
            this.Logger.LogWarning(
                "API key authentication failed for key: {MaskedApiKey} via {AuthMethod}",
                maskedKey,
                authMethod);

            // Record the failed attempt for rate limiting
            await apiKeyRateLimiter.RecordRequestAsync(apiKey);

            // Publish audit event for failed authentication
            await this.PublishAuthenticationAuditEventAsync(maskedKey, authMethod, false, "Invalid API key");

            return AuthenticateResult.Fail("Invalid API key");
        }

        this.Logger.LogInformation(
            "API key authentication successful for key: {MaskedApiKey} via {AuthMethod}",
            maskedKey,
            authMethod);

        // Record the successful request for rate limiting
        await apiKeyRateLimiter.RecordRequestAsync(apiKey);

        // Publish audit event for successful authentication
        await this.PublishAuthenticationAuditEventAsync(maskedKey, authMethod, true, null);

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

    private static Task<Guid?> GetApiKeyIdFromValidationServiceAsync()
    {
        // This is a temporary solution. In a real implementation,
        // the validation service should return the API key ID
        // For now, we'll return null and use Guid.Empty in the audit event
        return Task.FromResult<Guid?>(null);
    }

    private async Task PublishAuthenticationAuditEventAsync(string maskedKey, string authMethod, bool isSuccessful, string? failureReason)
    {
        try
        {
            // Get API key ID if possible (for successful authentications)
            Guid? apiKeyId = null;
            if (isSuccessful)
            {
                // We need to get the API key ID from the validation service
                // This is a bit of a hack, but we need the ID for audit logging
                // In a real implementation, the validation service could return more info
                apiKeyId = await GetApiKeyIdFromValidationServiceAsync();
            }

            var auditEvent = new ApiKeyAuthenticationAttemptedIntegrationEvent(
                ApiKeyId: apiKeyId ?? Guid.Empty,
                MaskedApiKey: maskedKey,
                AuthenticationMethod: authMethod,
                IsSuccessful: isSuccessful,
                FailureReason: failureReason,
                IpAddress: this.GetClientIpAddress(),
                UserAgent: this.GetUserAgent(),
                AttemptedAt: DateTimeOffset.UtcNow);

            await integrationEventPublisher.PublishAsync(auditEvent);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break authentication
            this.Logger.LogError(ex, "Failed to publish API key authentication audit event");
        }
    }

    private string? GetClientIpAddress()
    {
        // Try to get the real client IP address
        if (this.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        if (this.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.FirstOrDefault();
        }

        return this.Request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return this.Request.Headers.TryGetValue("User-Agent", out var userAgent)
            ? userAgent.FirstOrDefault()
            : null;
    }
}