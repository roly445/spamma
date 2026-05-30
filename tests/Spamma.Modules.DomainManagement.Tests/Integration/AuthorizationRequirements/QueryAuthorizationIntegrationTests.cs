using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Application.Authorizers.Queries;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Tests.Integration.AuthorizationRequirements;

public class QueryAuthorizationIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private Guid _domainId;
    private Guid _subdomain1Id;
    private Guid _subdomain2Id;
    private Guid _otherDomainId;
    private Guid _otherSubdomainId;

    public QueryAuthorizationIntegrationTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        this._domainId = Guid.NewGuid();
        this._subdomain1Id = Guid.NewGuid();
        this._subdomain2Id = Guid.NewGuid();
        this._otherDomainId = Guid.NewGuid();
        this._otherSubdomainId = Guid.NewGuid();

        this._fixture.Session!.Store(new SubdomainLookup
        {
            Id = this._subdomain1Id,
            DomainId = this._domainId,
            SubdomainName = "app",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "app.example.com",
            ParentName = "example.com",
        });

        this._fixture.Session.Store(new SubdomainLookup
        {
            Id = this._subdomain2Id,
            DomainId = this._domainId,
            SubdomainName = "api",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "api.example.com",
            ParentName = "example.com",
        });

        this._fixture.Session.Store(new SubdomainLookup
        {
            Id = this._otherSubdomainId,
            DomainId = this._otherDomainId,
            SubdomainName = "app",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "app.otherdomain.com",
            ParentName = "otherdomain.com",
        });

        await this._fixture.Session.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateSubdomainDetails_WhenUserModeratesParentDomain_Succeeds()
    {
        var authorizer = new UpdateSubdomainDetailsCommandAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser(moderatedDomains: [this._domainId])), this._fixture.Session!);
        var command = new UpdateSubdomainDetailsCommand(this._subdomain1Id, "updated");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSubdomainDetails_WhenUserModeratesOtherDomain_Fails()
    {
        var authorizer = new UpdateSubdomainDetailsCommandAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser(moderatedDomains: [this._otherDomainId])), this._fixture.Session!);
        var command = new UpdateSubdomainDetailsCommand(this._subdomain1Id, "updated");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSubdomainDetails_WhenSystemAdministrator_Succeeds()
    {
        var authorizer = new UpdateSubdomainDetailsCommandAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser(systemRole: SystemRole.DomainManagement)), this._fixture.Session!);
        var command = new UpdateSubdomainDetailsCommand(this._subdomain1Id, "updated");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task GetChaosAddressBySubdomainAndLocalPart_WhenUserViewsSubdomain_Succeeds()
    {
        var authorizer = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser(viewableSubdomains: [this._subdomain1Id])), this._fixture.Session!);
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(this._subdomain1Id, "chaos");

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task GetChaosAddressBySubdomainAndLocalPart_WhenUserHasNoAccess_Fails()
    {
        var authorizer = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser()), this._fixture.Session!);
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(this._subdomain1Id, "chaos");

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task GetChaosAddressBySubdomainAndLocalPart_WhenUserDirectlyModeratesSubdomain_Succeeds()
    {
        var authorizer = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser(moderatedSubdomains: [this._subdomain1Id])), this._fixture.Session!);
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(this._subdomain1Id, "chaos");

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task GetChaosAddressBySubdomainAndLocalPart_CrossDomainIsolation_EnforcesStrictBoundaries()
    {
        var authorizer = new GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer(CreateHttpContextAccessor(CreateAuthenticatedUser(moderatedDomains: [this._domainId])), this._fixture.Session!);
        var query = new GetChaosAddressBySubdomainAndLocalPartQuery(this._otherSubdomainId, "chaos");

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task SearchSubdomains_WhenInternalQueryIsStored_Succeeds()
    {
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var internalQueryStore = new InternalQueryStore();
        internalQueryStore.StoreQueryRef(query);
        var authorizer = new SearchSubdomainsQueryAuthorizer(internalQueryStore, CreateHttpContextAccessor(CreateUnauthenticatedHttpContext()));

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task SearchSubdomains_WhenUnauthenticated_Fails()
    {
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var authorizer = new SearchSubdomainsQueryAuthorizer(new InternalQueryStore(), CreateHttpContextAccessor(CreateUnauthenticatedHttpContext()));

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task SearchSubdomains_WhenUserModeratesDomain_Succeeds()
    {
        var query = new SearchSubdomainsQuery(null, null, null, 1, 10, "Name", false);
        var authorizer = new SearchSubdomainsQueryAuthorizer(new InternalQueryStore(), CreateHttpContextAccessor(CreateAuthenticatedUser(moderatedDomains: [this._domainId])));

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    private static UserAuthInfo CreateAuthenticatedUser(
        SystemRole systemRole = 0,
        Guid[]? moderatedDomains = null,
        Guid[]? moderatedSubdomains = null,
        Guid[]? viewableSubdomains = null)
    {
        return UserAuthInfo.Authenticated(
            Guid.NewGuid(),
            "User",
            "user@example.com",
            systemRole,
            moderatedDomains ?? [],
            moderatedSubdomains ?? [],
            viewableSubdomains ?? []);
    }

    private static HttpContextAccessor CreateHttpContextAccessor(UserAuthInfo userAuthInfo)
    {
        return new HttpContextAccessor
        {
            HttpContext = CreateAuthenticatedHttpContext(userAuthInfo),
        };
    }

    private static HttpContextAccessor CreateHttpContextAccessor(HttpContext httpContext)
    {
        return new HttpContextAccessor
        {
            HttpContext = httpContext,
        };
    }

    private static HttpContext CreateAuthenticatedHttpContext(UserAuthInfo userAuthInfo)
    {
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userAuthInfo.UserId.ToString()),
            new(ClaimTypes.Name, userAuthInfo.Name ?? string.Empty),
            new(ClaimTypes.Email, userAuthInfo.EmailAddress ?? string.Empty),
            new(ClaimTypes.Role, userAuthInfo.SystemRole.ToString()),
        };

        foreach (var domainId in userAuthInfo.ModeratedDomains)
        {
            claims.Add(new Claim("moderated_domain", domainId.ToString()));
        }

        foreach (var subdomainId in userAuthInfo.ModeratedSubdomains)
        {
            claims.Add(new Claim("moderated_subdomain", subdomainId.ToString()));
        }

        foreach (var subdomainId in userAuthInfo.ViewableSubdomains)
        {
            claims.Add(new Claim("viewable_subdomain", subdomainId.ToString()));
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
        return httpContext;
    }

    private static HttpContext CreateUnauthenticatedHttpContext()
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };
    }
}
