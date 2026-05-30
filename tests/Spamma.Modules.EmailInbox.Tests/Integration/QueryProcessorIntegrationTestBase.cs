using System.Security.Claims;
using BluQube.Queries;
using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class QueryProcessorIntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlFixture? _fixture;
    private IQueryRunner? _querier;
    private MockHttpContextAccessor? _httpContextAccessor;
    private IServiceProvider? _serviceProvider;

    protected IQueryRunner Sender => this._querier ?? throw new InvalidOperationException("Sender not initialized");

    protected IQueryRunner Querier => this.Sender;

    protected IDocumentSession Session => this._fixture?.Session ?? throw new InvalidOperationException("Fixture not initialized");

    protected MockHttpContextAccessor HttpContextAccessor => this._httpContextAccessor ?? throw new InvalidOperationException("HttpContextAccessor not initialized");

    protected IServiceProvider ServiceProvider => this._serviceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized");

    public async Task InitializeAsync()
    {
        this._fixture = new PostgreSqlFixture();
        await this._fixture.InitializeAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IQueryRunner, QueryRunner>();
        services.AddSingleton<IInternalQueryStore, InternalQueryStore>();
        services.AddTransient<Spamma.Modules.Common.Application.Contracts.IDirectoryWrapper, Spamma.Modules.Common.Application.Contracts.DirectoryWrapper>();
        services.AddTransient<Spamma.Modules.Common.Application.Contracts.IFileWrapper, Spamma.Modules.Common.Application.Contracts.FileWrapper>();

        services.AddMarten(opts =>
        {
            opts.Connection(this._fixture.ConnectionString!);
            opts.DatabaseSchemaName = "public";
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(opts);
        });

        services.AddEmailInbox();

        this._httpContextAccessor = new MockHttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(this._httpContextAccessor);

        var provider = services.BuildServiceProvider();
        this._serviceProvider = provider;
        this._querier = provider.GetRequiredService<IQueryRunner>();
    }

    public async Task DisposeAsync()
    {
        if (this._fixture != null)
        {
            await this._fixture.DisposeAsync();
        }
    }

    protected async Task<CampaignSummary> CreateCampaignAsync(
        Guid? subdomainId = null,
        int totalCaptured = 5,
        CancellationToken cancellationToken = default)
    {
        return await TestDataSeeder.CreateCampaignAsync(
            this.Session!,
            subdomainId: subdomainId ?? Guid.NewGuid(),
            totalCaptured: totalCaptured,
            cancellationToken: cancellationToken);
    }

    protected async Task<List<CampaignSummary>> CreateCampaignsAsync(
        Guid subdomainId,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        return await TestDataSeeder.CreateCampaignsAsync(
            this.Session!,
            subdomainId,
            count,
            cancellationToken);
    }

    protected async Task<EmailLookup> CreateEmailAsync(
        Guid? subdomainId = null,
        string? subject = null,
        CancellationToken cancellationToken = default)
    {
        return await TestDataSeeder.CreateEmailAsync(
            this.Session!,
            subdomainId: subdomainId ?? Guid.NewGuid(),
            subject: subject,
            cancellationToken: cancellationToken);
    }

    protected async Task<List<EmailLookup>> CreateEmailsAsync(
        Guid subdomainId,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        return await TestDataSeeder.CreateEmailsAsync(
            this.Session!,
            subdomainId,
            count,
            cancellationToken);
    }

    protected void PersistEmailAddresses(EmailLookup email)
    {
        // Email address persistence is handled by the seeded read models used in these tests.
    }

    protected class MockHttpContextAccessor : IHttpContextAccessor
    {
        private readonly List<Guid> _subdomainIds = new();

        public MockHttpContextAccessor()
        {
            this.UpdateHttpContext();
        }

        public HttpContext? HttpContext { get; set; }

        public void AddSubdomainClaim(Guid subdomainId)
        {
            this._subdomainIds.Add(subdomainId);
            this.UpdateHttpContext();
        }

        public void ClearSubdomainClaims()
        {
            this._subdomainIds.Clear();
            this.UpdateHttpContext();
        }

        private void UpdateHttpContext()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Email, "test@example.com"),
                new(ClaimTypes.Role, SystemRole.DomainManagement.ToString()),
            };

            foreach (var id in this._subdomainIds)
            {
                claims.Add(new Claim(Lookups.ViewableSubdomainClaim, id.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            this.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            };
        }
    }
}