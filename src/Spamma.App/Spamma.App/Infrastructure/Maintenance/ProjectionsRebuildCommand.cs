using Marten;

namespace Spamma.App.Infrastructure.Maintenance;

public class ProjectionsRebuildCommand
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<ProjectionsRebuildCommand> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    public ProjectionsRebuildCommand(
        IDocumentStore documentStore,
        ILogger<ProjectionsRebuildCommand> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        this._documentStore = documentStore;
        this._logger = logger;
        this._hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task Execute()
    {
        this._logger.LogInformation("Starting projection rebuild from event store...");

        try
        {
            // Delete all projection/read model documents to clear state
            using (var session = this._documentStore.LightweightSession())
            {
                this._logger.LogInformation("Clearing all projection data...");
                await session.Database.DeleteAllDocumentsAsync();
            }

            this._logger.LogInformation("Cleared all projection tables. Application will rebuild projections on next startup.");
            Console.WriteLine(string.Empty);
            Console.WriteLine("✓ Projection data cleared successfully");
            Console.WriteLine("ℹ Restart the application to rebuild projections from the event store");
            Console.WriteLine("ℹ The application automatically rebuilds projections during startup via ApplyAllDatabaseChangesOnStartup()");
            Console.WriteLine(string.Empty);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error clearing projections");
            Console.WriteLine($"✗ Error clearing projections: {ex.Message}");
        }
        finally
        {
            // Stop the application after command completes
            this._hostApplicationLifetime.StopApplication();
        }
    }
}

