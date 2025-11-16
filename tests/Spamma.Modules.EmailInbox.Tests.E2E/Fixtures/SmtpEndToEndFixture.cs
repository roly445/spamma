using System.Net;
using System.Net.Sockets;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.DomainManagement.Client;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;

public class SmtpEndToEndFixture : IAsyncLifetime
{
    private IHost? _host;

    public IServiceProvider ServiceProvider => this._host?.Services ?? throw new InvalidOperationException("Fixture not initialized");

    public int SmtpServerPort { get; private set; } = 1025; // Standard SMTP test port

    public PostgreSqlContainer PostgresContainer { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // 1. Start PostgreSQL container
        this.PostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("spamma_e2e_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await StartContainerWithRetryAsync(this.PostgresContainer);

        // 2. Build real service host with all modules
        var builder = Host.CreateApplicationBuilder();

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Configure Marten with PostgreSQL container
        builder.Services.AddMarten(options =>
        {
            options.Connection(this.PostgresContainer.GetConnectionString());
            options.DatabaseSchemaName = "public";

            // Configure module projections
            Spamma.Modules.DomainManagement.Module.ConfigureDomainManagement(options);
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(options);
        });

        // Register TimeProvider (required by background services)
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

        // Register BluQube CQRS infrastructure
        builder.Services.AddScoped<BluQube.Commands.ICommander, BluQube.Commands.Commander>();
        builder.Services.AddScoped<BluQube.Queries.IQuerier, BluQube.Queries.Querier>();

        // Register real modules
        builder.Services
            .AddDomainManagement()
            .AddEmailInbox();

        this._host = builder.Build();

        // 3. Apply Marten schema migrations
        var store = this._host.Services.GetRequiredService<IDocumentStore>();
        await store.Advanced.Clean.CompletelyRemoveAllAsync();
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        // 4. Start hosted services (including SMTP server)
        await this._host.StartAsync();

        // Wait for SMTP server to be ready
        await this.WaitForSmtpServerAsync();

        // 5. Seed test data
        await this.SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        if (this._host != null)
        {
            await this._host.StopAsync();
            this._host.Dispose();
        }

        await this.PostgresContainer.DisposeAsync();
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

    private async Task WaitForSmtpServerAsync()
    {
        var maxAttempts = 30;
        var delay = TimeSpan.FromMilliseconds(100);

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, this.SmtpServerPort);
                return; // Success
            }
            catch
            {
                if (i == maxAttempts - 1)
                {
                    throw new InvalidOperationException($"SMTP server did not start on port {this.SmtpServerPort} after {maxAttempts} attempts");
                }

                await Task.Delay(delay);
            }
        }
    }

    private async Task SeedTestDataAsync()
    {
        if (this._host == null)
        {
            throw new InvalidOperationException("Host is not initialized. Call InitializeAsync first.");
        }

        using var scope = this._host.Services.CreateScope();
        var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = documentStore.LightweightSession();

        // Create test domain and subdomain IDs
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Create test domain stream with typed event
        session.Events.StartStream<Spamma.Modules.DomainManagement.Domain.DomainAggregate.Domain>(
            domainId,
            new Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events.DomainCreated(
                domainId,
                "example.com",
                null,
                "Test domain for E2E tests",
                Guid.NewGuid().ToString("N"),
                now));

        // Create test subdomain stream with typed event
        session.Events.StartStream<Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Subdomain>(
            subdomainId,
            new Spamma.Modules.DomainManagement.Domain.SubdomainAggregate.Events.SubdomainCreated(
                subdomainId,
                domainId,
                "spamma",
                now,
                "Test subdomain for E2E tests"));

        // Create enabled chaos address
        var chaosAddressEnabledId = Guid.NewGuid();
        session.Events.StartStream<Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.ChaosAddress>(
            chaosAddressEnabledId,
            new Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events.ChaosAddressCreated(
                chaosAddressEnabledId,
                domainId,
                subdomainId,
                "chaos",
                Spamma.Modules.Common.Client.SmtpResponseCode.MailboxUnavailable,
                now));

        // Create disabled chaos address
        var chaosAddressDisabledId = Guid.NewGuid();
        session.Events.StartStream<Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.ChaosAddress>(
            chaosAddressDisabledId,
            new Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events.ChaosAddressCreated(
                chaosAddressDisabledId,
                domainId,
                subdomainId,
                "disabled",
                Spamma.Modules.Common.Client.SmtpResponseCode.MailboxUnavailable,
                now),
            new Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events.ChaosAddressDisabled(now));

        await session.SaveChangesAsync();

        // Trigger projection daemon to process events and update lookup tables
        var daemon = await documentStore.BuildProjectionDaemonAsync();
        await daemon.StartAllAsync();
        await daemon.WaitForNonStaleData(TimeSpan.FromSeconds(5));
    }
}