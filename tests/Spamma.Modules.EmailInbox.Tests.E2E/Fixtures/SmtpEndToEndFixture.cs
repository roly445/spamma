using System.Net;
using System.Net.Sockets;
using Dapper;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.DomainManagement.Client;
using Testcontainers.PostgreSql;

namespace Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;

/// <summary>
/// End-to-end test fixture providing:
/// - Real PostgreSQL container (Testcontainers)
/// - Real SMTP server on port 1025 (standard test port)
/// - Real Marten event store with projections
/// - Real module services (caches, command/query handlers).
/// Note: Test data must be seeded via SQL or command handlers in individual tests.
/// </summary>
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

        await this.PostgresContainer.StartAsync();

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

    /// <summary>
    /// Seeds test data for E2E tests using direct SQL.
    /// Creates: domain "example.com", subdomain "spamma.example.com", chaos addresses.
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        if (this._host == null)
        {
            throw new InvalidOperationException("Host is not initialized. Call InitializeAsync first.");
        }

        using var scope = this._host.Services.CreateScope();
        var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = documentStore.LightweightSession();

        // Create test domain and subdomain using direct SQL
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Insert domain events
        await session.Connection.ExecuteAsync(
            @"INSERT INTO public.mt_events (id, seq_id, stream_id, version, data, type, timestamp, tenant_id, mt_dotnet_type)
              VALUES (@EventId1, nextval('public.mt_events_sequence'), @StreamId, 1, @Data1, 'domain_created', @Timestamp, '*DEFAULT*', 'Spamma.Modules.DomainManagement.Domain.DomainAggregate.DomainCreated, Spamma.Modules.DomainManagement'),
                     (@EventId2, nextval('public.mt_events_sequence'), @StreamId, 2, @Data2, 'subdomain_created', @Timestamp, '*DEFAULT*', 'Spamma.Modules.DomainManagement.Domain.DomainAggregate.SubdomainCreated, Spamma.Modules.DomainManagement')",
            new
            {
                EventId1 = Guid.NewGuid(),
                EventId2 = Guid.NewGuid(),
                StreamId = domainId,
                Data1 = $@"{{""DomainId"":""{domainId}"",""Hostname"":""example.com"",""CreatedBy"":""{userId}"",""CreatedAt"":""{DateTime.UtcNow:O}""}}",
                Data2 = $@"{{""DomainId"":""{domainId}"",""SubdomainId"":""{subdomainId}"",""Subdomain"":""spamma"",""Status"":1,""CreatedBy"":""{userId}"",""CreatedAt"":""{DateTime.UtcNow:O}""}}",
                Timestamp = DateTime.UtcNow,
            });

        // Create chaos addresses using direct SQL
        var chaosAddressEnabledId = Guid.NewGuid();
        var chaosAddressDisabledId = Guid.NewGuid();

        await session.Connection.ExecuteAsync(
            @"INSERT INTO public.mt_events (id, seq_id, stream_id, version, data, type, timestamp, tenant_id, mt_dotnet_type)
              VALUES (@EventId1, nextval('public.mt_events_sequence'), @StreamId1, 1, @Data1, 'chaos_address_created', @Timestamp, '*DEFAULT*', 'Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.ChaosAddressCreated, Spamma.Modules.DomainManagement'),
                     (@EventId2, nextval('public.mt_events_sequence'), @StreamId2, 1, @Data2, 'chaos_address_created', @Timestamp, '*DEFAULT*', 'Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.ChaosAddressCreated, Spamma.Modules.DomainManagement'),
                     (@EventId3, nextval('public.mt_events_sequence'), @StreamId2, 2, @Data3, 'chaos_address_disabled', @Timestamp, '*DEFAULT*', 'Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.ChaosAddressDisabled, Spamma.Modules.DomainManagement')",
            new
            {
                EventId1 = Guid.NewGuid(),
                EventId2 = Guid.NewGuid(),
                EventId3 = Guid.NewGuid(),
                StreamId1 = chaosAddressEnabledId,
                StreamId2 = chaosAddressDisabledId,
                Data1 = $@"{{""ChaosAddressId"":""{chaosAddressEnabledId}"",""SubdomainId"":""{subdomainId}"",""LocalPart"":""chaos"",""SmtpResponseCode"":450,""SmtpResponseMessage"":""Mailbox unavailable"",""CreatedBy"":""{userId}"",""CreatedAt"":""{DateTime.UtcNow:O}""}}",
                Data2 = $@"{{""ChaosAddressId"":""{chaosAddressDisabledId}"",""SubdomainId"":""{subdomainId}"",""LocalPart"":""disabled"",""SmtpResponseCode"":450,""SmtpResponseMessage"":""Mailbox unavailable"",""CreatedBy"":""{userId}"",""CreatedAt"":""{DateTime.UtcNow:O}""}}",
                Data3 = $@"{{""ChaosAddressId"":""{chaosAddressDisabledId}"",""DisabledBy"":""{userId}"",""DisabledAt"":""{DateTime.UtcNow:O}""}}",
                Timestamp = DateTime.UtcNow,
            });

        await session.SaveChangesAsync();
    }
}

