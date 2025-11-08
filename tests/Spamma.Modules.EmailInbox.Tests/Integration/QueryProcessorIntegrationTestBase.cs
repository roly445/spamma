using System.Security.Claims;
using BluQube.Queries;
using Marten;
using MediatR;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Application;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

/// <summary>
/// Base class for query processor integration tests using PostgreSQL testcontainer.
/// Provides setup for dependencies, test data seeding, and query execution via ISender.
/// </summary>
public class QueryProcessorIntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlFixture? _fixture;
    private ISender? _sender;
    private MockHttpContextAccessor? _httpContextAccessor;
    private IServiceProvider? _serviceProvider;

    protected ISender Sender => this._sender ?? throw new InvalidOperationException("Sender not initialized");

    /// <summary>
    /// Gets the querier (alias for Sender for backward compatibility with existing tests).
    /// </summary>
    protected ISender Querier => this.Sender;

    protected IDocumentSession Session => this._fixture?.Session ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// Gets the mock HTTP context accessor for setting subdomain claims.
    /// Use AddSubdomainClaim() to add subdomain IDs that tests can access via SearchEmailsQuery.
    /// </summary>
    protected MockHttpContextAccessor HttpContextAccessor => this._httpContextAccessor ?? throw new InvalidOperationException("HttpContextAccessor not initialized");

    /// <summary>
    /// Gets the service provider for accessing registered services like IMessageStoreProvider.
    /// </summary>
    protected IServiceProvider ServiceProvider => this._serviceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized");

    public async Task InitializeAsync()
    {
        this._fixture = new PostgreSqlFixture();
        await this._fixture.InitializeAsync();

        var services = new ServiceCollection();

        // Add logging services (required by LocalMessageStoreProvider)
        services.AddLogging();

        // Add file system wrappers (required by LocalMessageStoreProvider)
        services.AddTransient<Spamma.Modules.Common.Application.Contracts.IDirectoryWrapper, Spamma.Modules.Common.Application.Contracts.DirectoryWrapper>();
        services.AddTransient<Spamma.Modules.Common.Application.Contracts.IFileWrapper, Spamma.Modules.Common.Application.Contracts.FileWrapper>();

        services.AddMarten(opts =>
        {
            opts.Connection(this._fixture.ConnectionString!);
            opts.DatabaseSchemaName = "public";

            // Configure EmailInbox projections and document mappings
            Spamma.Modules.EmailInbox.Module.ConfigureEmailInbox(opts);
        })
        .UseLightweightSessions();

        services.AddEmailInbox(); // Server module - registers query processors via MediatR

        // Register HTTP context accessor with mock HttpContext containing subdomain claims
        this._httpContextAccessor = new MockHttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(this._httpContextAccessor);

        // Replace real authorization handlers with mock ones that always succeed (for testing)
        var mustBeAuthDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationHandler<MustBeAuthenticatedRequirement>));
        if (mustBeAuthDescriptor != null)
        {
            services.Remove(mustBeAuthDescriptor);
        }

        services.AddTransient<IAuthorizationHandler<MustBeAuthenticatedRequirement>, AlwaysAuthorizeHandler>();

        var campaignAccessDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationHandler<MustHaveAccessToAtLeastOneCampaignRequirement>));
        if (campaignAccessDescriptor != null)
        {
            services.Remove(campaignAccessDescriptor);
        }

        services.AddTransient<IAuthorizationHandler<MustHaveAccessToAtLeastOneCampaignRequirement>, AlwaysAuthorizeCampaignAccessHandler>();

        var mustHaveAccessToCampaignDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationHandler<MustHaveAccessToCampaignRequirement>));
        if (mustHaveAccessToCampaignDescriptor != null)
        {
            services.Remove(mustHaveAccessToCampaignDescriptor);
        }

        services.AddTransient<IAuthorizationHandler<MustHaveAccessToCampaignRequirement>, AlwaysAuthorizeSpecificCampaignAccessHandler>();

        var mustHaveAccessToSubdomainDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationHandler<MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement>));
        if (mustHaveAccessToSubdomainDescriptor != null)
        {
            services.Remove(mustHaveAccessToSubdomainDescriptor);
        }

        services.AddTransient<IAuthorizationHandler<MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement>, AlwaysAuthorizeSubdomainEmailAccessHandler>();

        var mustHaveAccessToSubdomainViaEmailDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationHandler<MustHaveAccessToSubdomainViaEmailRequirement>));
        if (mustHaveAccessToSubdomainViaEmailDescriptor != null)
        {
            services.Remove(mustHaveAccessToSubdomainViaEmailDescriptor);
        }

        services.AddTransient<IAuthorizationHandler<MustHaveAccessToSubdomainViaEmailRequirement>, AlwaysAuthorizeSubdomainViaEmailAccessHandler>();

        var provider = services.BuildServiceProvider();
        this._serviceProvider = provider;
        this._sender = provider.GetRequiredService<ISender>();
    }

    public async Task DisposeAsync()
    {
        if (this._fixture != null)
        {
            await this._fixture.DisposeAsync();
        }
    }

    /// <summary>
    /// Helper to create a campaign for testing.
    /// Returns the created CampaignSummary.
    /// </summary>
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

    /// <summary>
    /// Helper to create multiple campaigns for testing.
    /// Returns the list of created CampaignSummary objects.
    /// </summary>
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

    /// <summary>
    /// Helper to create an email for testing.
    /// Returns the created EmailLookup.
    /// </summary>
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

    /// <summary>
    /// Helper to create multiple emails for testing.
    /// Returns the list of created EmailLookup objects.
    /// </summary>
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

    /// <summary>
    /// Mock IHttpContextAccessor that returns an HttpContext with subdomain claims.
    /// Tests can call AddSubdomainClaim() to add subdomain IDs that SearchEmailsQuery will use for filtering.
    /// </summary>
    protected class MockHttpContextAccessor : IHttpContextAccessor
    {
        private readonly List<Guid> _subdomainIds = new();

        public HttpContext? HttpContext { get; set; }

        /// <summary>
        /// Adds a subdomain claim to the mock HttpContext.
        /// SearchEmailsQuery will filter emails to only include this subdomain.
        /// </summary>
        /// <param name="subdomainId">Subdomain ID to add as a claim.</param>
        public void AddSubdomainClaim(Guid subdomainId)
        {
            this._subdomainIds.Add(subdomainId);
            this.UpdateHttpContext();
        }

        /// <summary>
        /// Clears all subdomain claims from the mock HttpContext.
        /// </summary>
        public void ClearSubdomainClaims()
        {
            this._subdomainIds.Clear();
            this.UpdateHttpContext();
        }

        private void UpdateHttpContext()
        {
            var claims = this._subdomainIds.Select(id => new Claim(Lookups.ViewableSubdomainClaim, id.ToString())).ToList();
            this.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
            };
        }
    }

    /// <summary>
    /// Mock authorization handler that always succeeds for tests.
    /// </summary>
    private class AlwaysAuthorizeHandler : IAuthorizationHandler<MustBeAuthenticatedRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeAuthenticatedRequirement requirement, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }
    }

    /// <summary>
    /// Mock authorization handler for campaign access that always succeeds for tests.
    /// </summary>
    private class AlwaysAuthorizeCampaignAccessHandler : IAuthorizationHandler<MustHaveAccessToAtLeastOneCampaignRequirement>
    {
        public Task<AuthorizationResult> Handle(MustHaveAccessToAtLeastOneCampaignRequirement requirement, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }
    }

    /// <summary>
    /// Mock authorization handler for specific campaign access that always succeeds for tests.
    /// </summary>
    private class AlwaysAuthorizeSpecificCampaignAccessHandler : IAuthorizationHandler<MustHaveAccessToCampaignRequirement>
    {
        public Task<AuthorizationResult> Handle(MustHaveAccessToCampaignRequirement requirement, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }
    }

    /// <summary>
    /// Mock authorization handler for subdomain email access that always succeeds for tests.
    /// </summary>
    private class AlwaysAuthorizeSubdomainEmailAccessHandler : IAuthorizationHandler<MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement>
    {
        public Task<AuthorizationResult> Handle(MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement requirement, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }
    }

    /// <summary>
    /// Mock authorization handler for subdomain via email access that always succeeds for tests.
    /// </summary>
    private class AlwaysAuthorizeSubdomainViaEmailAccessHandler : IAuthorizationHandler<MustHaveAccessToSubdomainViaEmailRequirement>
    {
        public Task<AuthorizationResult> Handle(MustHaveAccessToSubdomainViaEmailRequirement requirement, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }
    }
}
