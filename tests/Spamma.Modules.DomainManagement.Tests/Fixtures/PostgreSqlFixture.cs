using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.DomainManagement.Tests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private IDocumentStore? _store;

    public IDocumentSession? Session { get; private set; }

    public string? ConnectionString { get; private set; }

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

            // Configure DomainManagement projections and document identity mappings
            Spamma.Modules.DomainManagement.Module.ConfigureDomainManagement(opts);
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