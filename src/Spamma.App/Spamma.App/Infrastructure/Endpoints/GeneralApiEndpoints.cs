using System.Security.Claims;
using MaybeMonad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;

namespace Spamma.App.Infrastructure.Endpoints;

/// <summary>
/// Extension methods for general API endpoints.
/// Includes user status queries and dynamic settings retrieval.
/// </summary>
internal static class GeneralApiEndpoints
{
    /// <summary>
    /// Maps general API endpoints including settings and user status.
    /// </summary>
    /// <param name="app">The web application.</param>
    internal static void MapGeneralApiEndpoints(this WebApplication app)
    {
        app.MapGet("dynamicsettings.json", GetDynamicSettings)
            .WithName("GetDynamicSettings")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("current-user", GetCurrentUser)
            .WithName("GetCurrentUser")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Returns dynamic application settings as JSON.
    /// Includes mail server configuration.
    /// </summary>
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

    /// <summary>
    /// Returns information about the currently authenticated user.
    /// Re-authenticates with updated claims to ensure session reflects latest user state.
    /// </summary>
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
}