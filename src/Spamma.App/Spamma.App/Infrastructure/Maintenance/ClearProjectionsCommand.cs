using Marten;

namespace Spamma.App.Infrastructure.Maintenance;

public class ClearProjectionsCommand(
    IDocumentStore documentStore,
    ILogger<ClearProjectionsCommand> logger)
{
    public async Task<int> Execute()
    {
        logger.LogInformation("Starting projection rebuild from event store...");

        try
        {
            // Delete all projection/read model documents to clear state
            await using (var session = documentStore.LightweightSession())
            {
                logger.LogInformation("Clearing all projection data...");
                await session.Database.DeleteAllDocumentsAsync();
            }

            logger.LogInformation("Cleared all projection tables. Application will rebuild projections on next startup.");
            Console.WriteLine(string.Empty);
            Console.WriteLine("✓ Projection data cleared successfully");
            Console.WriteLine("ℹ Restart the application to rebuild projections from the event store");
            Console.WriteLine("ℹ The application automatically rebuilds projections during startup via ApplyAllDatabaseChangesOnStartup()");
            Console.WriteLine(string.Empty);

            return 0; // Success
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing projections");
            Console.WriteLine($"✗ Error clearing projections: {ex.Message}");
            return 1; // Error
        }
    }
}