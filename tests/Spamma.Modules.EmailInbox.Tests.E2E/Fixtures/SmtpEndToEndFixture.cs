using System.Net;
using System.Net.Sockets;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.DomainManagement;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;

public class SmtpEndToEndFixture : IAsyncLifetime
{
    private IHost? _host;
    private string? _contentRoot;

    public IServiceProvider ServiceProvider => this._host?.Services ?? throw new InvalidOperationException("Fixture not initialized");

    public int SmtpServerPort { get; private set; }

    public PostgreSqlContainer PostgresContainer { get; private set; } = null!;

    public RedisContainer RedisContainer { get; private set; } = null!;

    public Guid DomainId { get; private set; }

    public Guid SubdomainId { get; private set; }

    public Guid ChaosAddressId { get; private set; }

    public Task InitializeAsync() => this.InitializeAsync(enableSubscriber: true);

    public async Task InitializeAsync(bool enableSubscriber, int? retryCount = null,
        Action<IServiceCollection>? configureServices = null)
    {
        // Both stores are disposable and are used by the real SMTP/CAP path.
        this.PostgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("spamma_e2e_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
        this.RedisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();

        await StartContainerWithRetryAsync(this.PostgresContainer);
        await this.RedisContainer.StartAsync();

        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            this.SmtpServerPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        this._contentRoot = Path.Combine(Path.GetTempPath(), $"spamma-smtp-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._contentRoot);

        this._host = this.BuildHost(enableSubscriber, retryCount, configureServices);

        var store = this._host.Services.GetRequiredService<IDocumentStore>();
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        await this.SeedTestDataAsync();

        await this._host.StartAsync();
        await this.WaitForSmtpServerAsync();
    }

    public async Task RestartAsync(bool enableSubscriber = true, int? retryCount = null,
        Action<IServiceCollection>? configureServices = null, Action? afterStop = null)
    {
        if (this._host == null)
        {
            throw new InvalidOperationException("The SMTP fixture has not started.");
        }

        await this._host.StopAsync();
        this._host.Dispose();
        afterStop?.Invoke();
        this._host = this.BuildHost(enableSubscriber, retryCount, configureServices);
        await this._host.StartAsync();
        await this.WaitForSmtpServerAsync();
    }

    public async Task DisposeAsync()
    {
        if (this._host != null)
        {
            await this._host.StopAsync();
            this._host.Dispose();
        }

        if (this.RedisContainer != null)
        {
            await this.RedisContainer.DisposeAsync();
        }

        await this.PostgresContainer.DisposeAsync();

        if (this._contentRoot != null && Directory.Exists(this._contentRoot))
        {
            Directory.Delete(this._contentRoot, recursive: true);
        }
    }

    public Task<EmailLookup> WaitForEmailAsync(string subject) =>
        this.WaitForEmailAsync(subject, TimeSpan.FromSeconds(40));

    public async Task<EmailLookup> WaitForEmailAsync(string subject, TimeSpan wait)
    {
        var timeout = DateTime.UtcNow.Add(wait);
        while (DateTime.UtcNow < timeout)
        {
            await using var session = this.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
            var email = await session.Query<EmailLookup>().FirstOrDefaultAsync(x => x.Subject == subject);
            if (email != null)
            {
                return email;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"SMTP message '{subject}' was not persisted before the test timeout.");
    }

    public async Task<CampaignSummary> WaitForCampaignAsync(string campaignValue)
    {
        var timeout = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < timeout)
        {
            await using var session = this.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
            var campaign = await session.Query<CampaignSummary>()
                .FirstOrDefaultAsync(x => x.CampaignValue == campaignValue);
            if (campaign != null)
            {
                return campaign;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"SMTP campaign '{campaignValue}' was not projected within 40 seconds.");
    }

    public async Task<ChaosAddressLookup> WaitForChaosCaptureAsync()
    {
        var timeout = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < timeout)
        {
            await using var session = this.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
            var chaosAddress = await session.LoadAsync<ChaosAddressLookup>(this.ChaosAddressId);
            if (chaosAddress is { TotalReceived: > 0 })
            {
                return chaosAddress;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("Chaos address receipt was not projected within 40 seconds.");
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

    private IHost BuildHost(bool enableSubscriber, int? retryCount,
        Action<IServiceCollection>? configureServices)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = this._contentRoot,
        });

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SmtpServer:Port"] = this.SmtpServerPort.ToString(),
        });
        builder.Services.Configure<EmailInboxSettings>(builder.Configuration.GetSection("SmtpServer"));

        // Configure Marten with PostgreSQL container
        builder.Services.AddMarten(options =>
        {
            options.Connection(this.PostgresContainer.GetConnectionString());
            options.RestoreV8Defaults();
            options.DatabaseSchemaName = "public";

            // Configure module projections
            Spamma.Modules.DomainManagement.Module.ConfigureDomainManagement(options);
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(options);
        }).UseIdentitySessions();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IInternalQueryStore, InternalQueryStore>();
        builder.Services.AddSingleton<IDirectoryWrapper, DirectoryWrapper>();
        builder.Services.AddSingleton<IFileWrapper, FileWrapper>();
        builder.Services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(this.RedisContainer.GetConnectionString()));

        var cap = builder.Services.AddCap(options =>
        {
            options.UseStorageLock = true;
            options.UseRedis(this.RedisContainer.GetConnectionString());
            options.UsePostgreSql(this.PostgresContainer.GetConnectionString());
            if (retryCount.HasValue)
            {
                options.FailedRetryCount = retryCount.Value;
                options.FailedRetryInterval = 1;
                // CAP otherwise waits four minutes before polling failed receipts.
                options.FallbackWindowLookbackSeconds = 1;
            }
        });
        cap.AddSubscriberAssembly(typeof(Spamma.Modules.EmailInbox.Module).Assembly);
        if (!enableSubscriber)
        {
            cap.AddSubscribeFilter<DeferSmtpCaptureFilter>();
        }

        // Register BluQube CQRS infrastructure
        builder.Services.AddScoped<BluQube.Commands.ICommandRunner, BluQube.Commands.CommandRunner>();
        builder.Services.AddScoped<BluQube.Queries.IQueryRunner, BluQube.Queries.QueryRunner>();

        // Register real modules
        builder.Services
            .AddCommonBehaviors()
            .AddDomainManagement()
            .AddEmailInbox();

        configureServices?.Invoke(builder.Services);
        return builder.Build();
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
        var domainId = this.DomainId = Guid.NewGuid();
        var subdomainId = this.SubdomainId = Guid.NewGuid();
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
                now),
            new Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events.DomainVerified(now));

        // SubdomainLookupProjection loads its parent while projecting SubdomainCreated.
        await session.SaveChangesAsync();

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
        var chaosAddressEnabledId = this.ChaosAddressId = Guid.NewGuid();
        session.Events.StartStream<Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.ChaosAddress>(
            chaosAddressEnabledId,
            new Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events.ChaosAddressCreated(
                chaosAddressEnabledId,
                domainId,
                subdomainId,
                "chaos",
                Spamma.Modules.Common.Client.SmtpResponseCode.MailboxUnavailable,
                now),
            new Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events.ChaosAddressEnabled(now));

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

        await using var query = documentStore.QuerySession();
        var domain = await query.LoadAsync<DomainLookup>(domainId);
        if (domain is not { IsVerified: true })
        {
            throw new InvalidOperationException("The SMTP test domain was not projected as verified.");
        }

        var subdomain = await query.LoadAsync<SubdomainLookup>(subdomainId);
        if (subdomain?.FullName != "spamma.example.com")
        {
            throw new InvalidOperationException($"The SMTP test subdomain was projected as '{subdomain?.FullName}'.");
        }
    }
}
