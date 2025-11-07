using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

/// <summary>
/// Base fixture for integration tests using a PostgreSQL Testcontainer.
/// Provides a real Marten document session connected to a containerized database.
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private IDocumentStore? _store;
    public IDocumentSession? Session { get; private set; }

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("spamma_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        // Configure Marten with the test database
        var services = new ServiceCollection();
        services.AddMarten(opts =>
        {
            opts.Connection(connectionString);
            opts.DatabaseSchemaName = "public";

            // Allow Marten to auto-create projections for read models
            // by discovering event-sourced aggregates
        });

        var provider = services.BuildServiceProvider();
        _store = provider.GetRequiredService<IDocumentStore>();

        // Create schema and apply all migrations
        await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        Session = _store.LightweightSession();
    }

    public async Task DisposeAsync()
    {
        if (Session != null)
        {
            await Session.DisposeAsync();
        }

        if (_store != null)
        {
            _store.Dispose();
        }

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
