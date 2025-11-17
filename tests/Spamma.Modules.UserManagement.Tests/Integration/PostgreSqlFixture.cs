using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.UserManagement.Tests.Integration;

public class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private IDocumentStore? _store;

    public IDocumentSession? Session { get; private set; }

    public string? ConnectionString { get; private set; }

    public IDocumentStore? Store { get; private set; }

    public async Task InitializeAsync()
    {
        this._container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("spamma_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await this._container.StartAsync();

        this.ConnectionString = this._container.GetConnectionString();

        // Configure Marten with the test database
        var services = new ServiceCollection();
        services.AddMarten(opts =>
        {
            opts.Connection(this.ConnectionString);
            opts.DatabaseSchemaName = "public";

            // Configure UserManagement projections and document identity mappings
            Spamma.Modules.UserManagement.Module.ConfigureUserManagement(opts);
        });

        var provider = services.BuildServiceProvider();
        this._store = provider.GetRequiredService<IDocumentStore>();
        this.Store = this._store;

        // Create schema and apply all migrations
        await this._store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        this.Session = this._store.LightweightSession();
    }

    public async Task DisposeAsync()
    {
        this.Session?.Dispose();
        this._store?.Dispose();

        if (this._container != null)
        {
            await this._container.StopAsync();
        }
    }
}