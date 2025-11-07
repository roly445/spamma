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
        this._container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("spamma_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await this._container.StartAsync();

        var connectionString = this._container.GetConnectionString();

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
        this._store = provider.GetRequiredService<IDocumentStore>();

        // Create schema and apply all migrations
        await this._store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        this.Session = this._store.LightweightSession();
    }

    public async Task DisposeAsync()
    {
        if (this.Session != null)
        {
            await this.Session.DisposeAsync();
        }

        if (this._store != null)
        {
            this._store.Dispose();
        }

        if (this._container != null)
        {
            await this._container.StopAsync();
            await this._container.DisposeAsync();
        }
    }
}
