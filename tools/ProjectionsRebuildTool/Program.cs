// ProjectionsRebuildTool - Standalone tool to rebuild projections
// This allows rebuilding projections without starting the web server

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Marten;
using Marten.Schema;
using Npgsql;
using Spamma.App.Infrastructure.Configuration;
using Spamma.Modules.DomainManagement;
using Spamma.Modules.EmailInbox;
using Spamma.Modules.UserManagement;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")!;
var redisConnectionString = configuration.GetConnectionString("Redis");

// Create a minimal service collection just for this tool
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add Marten with minimal configuration
services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.AutoCreateSchemaObjects = AutoCreate.All;

    options.ConfigureUserManagement()
        .ConfigureDomainManagement()
        .ConfigureEmailInbox();
});

var provider = services.BuildServiceProvider();
var documentStore = provider.GetRequiredService<IDocumentStore>();
var logger = provider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting projection rebuild from event store...");

try
{
    // Clear all projection/read model documents
    using (var session = documentStore.LightweightSession())
    {
        logger.LogInformation("Clearing all projection data...");
        await session.Database.DeleteAllDocumentsAsync();
    }

    logger.LogInformation("Cleared all projection tables.");
    Console.WriteLine(string.Empty);
    Console.WriteLine("✓ Projection data cleared successfully");
    Console.WriteLine("ℹ Restart the application to rebuild projections from the event store");
    Console.WriteLine("ℹ The application automatically rebuilds projections during startup");
    Console.WriteLine(string.Empty);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error clearing projections");
    Console.WriteLine($"✗ Error clearing projections: {ex.Message}");
    Environment.Exit(1);
}
