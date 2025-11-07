using Marten;
using Marten.Events.Projections;

namespace Spamma.App.Infrastructure.Endpoints.Maintenance;

/// <summary>
/// Maintenance endpoints for database operations.
/// Note: These should be restricted to admin/local access in production.
/// </summary>
internal static class MaintenanceEndpoints
{
    internal static void MapMaintenanceEndpoints(this WebApplication app)
    {
        app.MapPost("api/admin/maintenance/rebuild-projections", RebuildProjections)
            .WithName("RebuildProjections")
            .WithDescription("Rebuild all projections from the event store")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> RebuildProjections(IDocumentStore documentStore, ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Starting manual projection rebuild...");

            // Get the projection daemon
            var daemon = await documentStore.BuildProjectionDaemonAsync();

            // List of all projections to rebuild
            var projections = new[]
            {
                "Spamma.Modules.UserManagement.Infrastructure.Projections.UserLookupProjection",
                "Spamma.Modules.UserManagement.Infrastructure.Projections.PasskeyProjection",
                "Spamma.Modules.DomainManagement.Infrastructure.Projections.DomainLookupProjection",
                "Spamma.Modules.DomainManagement.Infrastructure.Projections.SubdomainLookupProjection",
                "Spamma.Modules.DomainManagement.Infrastructure.Projections.ChaosAddressLookupProjection",
                "Spamma.Modules.EmailInbox.Infrastructure.Projections.EmailLookupProjection",
                "Spamma.Modules.EmailInbox.Infrastructure.Projections.CampaignSummaryProjection",
            };

            // Rebuild each projection
            foreach (var projectionName in projections)
            {
                logger.LogInformation("Rebuilding projection: {ProjectionName}", projectionName);
                await daemon.RebuildProjectionAsync(projectionName, CancellationToken.None);
            }

            logger.LogInformation("All projections rebuilt successfully via API");

            return Results.Ok(new
            {
                success = true,
                message = "All projections have been rebuilt from the event store",
                projectionsRebuilt = projections.Length,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rebuilding projections via API");

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
