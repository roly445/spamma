using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Spamma.App.Infrastructure.Endpoints.Setup;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;

namespace Spamma.App.Infrastructure.Endpoints;

internal static class GeneralApiEndpoints
{
    internal static void MapGeneralApiEndpoints(this WebApplication app)
    {
        app.MapGet("dynamicsettings.json", GetDynamicSettings)
            .WithName("GetDynamicSettings")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("current-user", GetCurrentUser)
            .WithName("GetCurrentUser")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGenerateCertificateEndpoint();
        app.MapGenerateCertificateStreamEndpoint();

        // OTLP trace collection endpoint for WASM client
        // WASM client sends traces here, server forwards to real OTLP collector
        app.MapPost("api/otel/traces", ForwardOtelTraces)
            .WithName("ForwardOtelTraces")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery(); // OTEL collector doesn't use CSRF tokens
    }

    private static IResult GetDynamicSettings(IOptions<Settings> settings)
    {
        return Results.Json(new
        {
            Settings = new
            {
                settings.Value.MailServerHostname,
                settings.Value.MxPriority,
            },
        });
    }

    private static async Task<IResult> GetCurrentUser(
        HttpContext httpContext,
        UserStatusCache userStatusCache)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var userResult = await userStatusCache.GetUserLookupAsync(userId);
        if (userResult.HasNoValue)
        {
            return Results.Unauthorized();
        }

        var cachedUser = userResult.Value;

        // Rebuild claims with current user state
        var claims = ClaimsBuilder.BuildClaims(cachedUser);

        // Re-authenticate with updated claims
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
            AllowRefresh = true,
        };

        await httpContext.SignOutAsync();
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties);

        return Results.Json(userResult.Value);
    }

    private static async Task<IResult> ForwardOtelTraces(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        try
        {
            // Get the OTLP collector endpoint from environment
            var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            if (string.IsNullOrEmpty(otelEndpoint))
            {
                // If not configured, just return OK (telemetry is optional)
                logger.LogWarning("OTEL_EXPORTER_OTLP_ENDPOINT not configured, WASM client traces will be discarded");
                return Results.Ok();
            }

            // Ensure endpoint ends with /v1/traces
            if (!otelEndpoint.EndsWith('/'))
            {
                otelEndpoint += "/";
            }

            var tracesEndpoint = $"{otelEndpoint}v1/traces";

            // Read the request body (protobuf traces)
            var memoryStream = new MemoryStream();
            await httpContext.Request.Body.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Forward to real OTLP collector
            using var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                tracesEndpoint,
                new StreamContent(memoryStream)
                {
                    Headers = { ContentType = new("application/x-protobuf"), },
                });

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to forward WASM traces to {Endpoint}: {Status}",
                    tracesEndpoint,
                    response.StatusCode);
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding WASM client traces");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}