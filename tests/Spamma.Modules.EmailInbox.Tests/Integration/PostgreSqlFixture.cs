using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

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

        await StartContainerWithRetryAsync(this._container!);

        this.ConnectionString = this._container.GetConnectionString();

        // Configure Marten with the test database
        var services = new ServiceCollection();
        services.AddMarten(opts =>
        {
            opts.Connection(this.ConnectionString);
            opts.DatabaseSchemaName = "public";

            // Configure EmailInbox projections and document identity mappings
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(opts);
        });

        var provider = services.BuildServiceProvider();
        this._store = provider.GetRequiredService<IDocumentStore>();

        // Create schema and apply all migrations
        await this._store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        this.Session = this._store.DirtyTrackedSession();
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

    private static async Task StartContainerWithRetryAsync(PostgreSqlContainer container, int maxAttempts = 3)
    {
        var delayMs = 1000;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await container.StartAsync();
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                // Named pipe timeouts and transient Docker errors can be transient - wait and retry.
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }
    }
}