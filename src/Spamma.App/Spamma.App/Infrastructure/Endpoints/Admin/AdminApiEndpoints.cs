using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Infrastructure.Endpoints.Admin;

internal static class AdminApiEndpoints
{
    internal static void MapAdminApiEndpoints(this WebApplication app)
    {
        app.MapPost("api/admin/maintenance", EnableMaintenanceMode)
            .RequireAuthorization()
            .WithName("EnableMaintenanceMode")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> EnableMaintenanceMode(
        HttpContext httpContext,
        IInMemorySetupAuthService setupAuth,
        ILogger<Program> logger)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        logger.LogWarning("Maintenance mode requested by user {UserId}", userId);

        setupAuth.EnableMaintenanceMode($"Maintenance mode requested by user {userId}");

        await httpContext.SignOutAsync();

        return Results.Ok();
    }
}
