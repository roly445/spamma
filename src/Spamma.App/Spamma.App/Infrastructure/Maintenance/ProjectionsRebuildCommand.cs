using Marten;

namespace Spamma.App.Infrastructure.Maintenance;

/// <summary>
/// JasperFx maintenance command to rebuild all Marten projections from the event store.
/// Usage: dotnet run --project src/Spamma.App/Spamma.App/Spamma.App.csproj -- projections rebuild.
/// </summary>
public class ProjectionsRebuildCommand
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<ProjectionsRebuildCommand> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionsRebuildCommand"/> class.
    /// </summary>
    /// <param name="documentStore">The Marten document store.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="hostApplicationLifetime">Host application lifetime for graceful shutdown.</param>
    public ProjectionsRebuildCommand(
        IDocumentStore documentStore,
        ILogger<ProjectionsRebuildCommand> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        this._documentStore = documentStore;
        this._logger = logger;
        this._hostApplicationLifetime = hostApplicationLifetime;
    }

    /// <summary>
    /// Executes the projection rebuild command.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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




