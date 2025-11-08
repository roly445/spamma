using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.DomainManagement.Tests.Fixtures;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Tests.Integration.AuthorizationRequirements;

/// <summary>
/// Integration tests for query authorization requirements that use database queries.
/// These tests validate authorization logic with real PostgreSQL (testcontainers) to ensure
/// complex authorization scenarios (e.g., parent-child domain relationships) work correctly.
/// </summary>
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
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Seed test data: create domains and subdomains with parent-child relationships
        _domainId = Guid.NewGuid();
        _subdomain1Id = Guid.NewGuid();
        _subdomain2Id = Guid.NewGuid();
        _otherDomainId = Guid.NewGuid();
        _otherSubdomainId = Guid.NewGuid();

        // Create subdomain lookups for authorization queries
        var subdomain1 = new SubdomainLookup
        {
            Id = _subdomain1Id,
            DomainId = _domainId,
            SubdomainName = "app",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "app.example.com",
            ParentName = "example.com",
        };

        var subdomain2 = new SubdomainLookup
        {
            Id = _subdomain2Id,
            DomainId = _domainId,
            SubdomainName = "api",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "api.example.com",
            ParentName = "example.com",
        };

        var otherSubdomain = new SubdomainLookup
        {
            Id = _otherSubdomainId,
            DomainId = _otherDomainId,
            SubdomainName = "app",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "app.otherdomain.com",
            ParentName = "otherdomain.com",
        };

        _fixture.Session!.Store(subdomain1);
        _fixture.Session.Store(subdomain2);
        _fixture.Session.Store(otherSubdomain);
        await _fixture.Session.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task MustHaveAccessToSubdomain_UserModeratesParentDomain_Succeeds()
    {
        // Arrange: User moderates domain, tries to access subdomain under that domain
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustHaveAccessToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { _domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustHaveAccessToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeTrue("user moderates parent domain so should have access to subdomain");
    }

    [Fact]
    public async Task MustHaveAccessToSubdomain_UserViewsSubdomain_Succeeds()
    {
        // Arrange: User has viewable subdomain claim (not moderator)
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustHaveAccessToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { _subdomain1Id });

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustHaveAccessToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeTrue("user has viewable subdomain claim");
    }

    [Fact]
    public async Task MustHaveAccessToSubdomain_UserModeratesOtherDomain_Fails()
    {
        // Arrange: User moderates different domain, tries to access subdomain
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustHaveAccessToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { _otherDomainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustHaveAccessToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeFalse("user moderates different domain, not the subdomain's parent domain");
    }

    [Fact]
    public async Task MustHaveAccessToSubdomain_UserHasNoAccess_Fails()
    {
        // Arrange: User has no domain, subdomain, or viewable claims
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustHaveAccessToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustHaveAccessToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeFalse("user has no access claims");
    }

    [Fact]
    public async Task MustBeModeratorToSubdomain_UserModeratesParentDomain_Succeeds()
    {
        // Arrange: User moderates domain, tries to moderate subdomain under that domain
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { _domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustBeModeratorToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeTrue("user moderates parent domain, so has moderator rights on subdomain");
    }

    [Fact]
    public async Task MustBeModeratorToSubdomain_UserModeratesMultipleSubdomainsUnderSameDomain_Succeeds()
    {
        // Arrange: User moderates parent domain, tries to access multiple subdomains
        var userId = Guid.NewGuid().ToString();
        var requirement1 = new MustBeModeratorToSubdomainRequirement { SubdomainId = _subdomain1Id };
        var requirement2 = new MustBeModeratorToSubdomainRequirement { SubdomainId = _subdomain2Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { _domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustBeModeratorToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result1 = await handler.Handle(requirement1, CancellationToken.None);
        var result2 = await handler.Handle(requirement2, CancellationToken.None);

        // Assert
        result1.IsAuthorized.Should().BeTrue("user moderates parent domain for subdomain1");
        result2.IsAuthorized.Should().BeTrue("user moderates parent domain for subdomain2");
    }

    [Fact]
    public async Task MustBeModeratorToSubdomain_UserViewsSubdomainButNotModerator_Fails()
    {
        // Arrange: User has viewable claim but not moderator claim
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { _subdomain1Id });

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustBeModeratorToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeFalse("user has viewable claim, not moderator claim");
    }

    [Fact]
    public async Task MustHaveAccessToSubdomain_UserDirectlyModeratesSubdomain_Succeeds()
    {
        // Arrange: User directly moderates subdomain (not via parent domain)
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustHaveAccessToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            new[] { _subdomain1Id },
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustHaveAccessToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeTrue("user directly moderates subdomain");
    }

    [Fact]
    public async Task MustBeModeratorToSubdomain_SystemAdministrator_AlwaysSucceeds()
    {
        // Arrange: System administrator (DomainManagement role)
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = _subdomain1Id };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "Admin",
            "admin@example.com",
            SystemRole.DomainManagement,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustBeModeratorToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeTrue("system administrator bypasses all checks");
    }

    [Fact]
    public async Task MustHaveAccessToSubdomain_CrossDomainIsolation_EnforcesStrictBoundaries()
    {
        // Arrange: User moderates one domain, tries to access subdomain from different domain
        // This is a critical security test - ensures users cannot access resources across tenant boundaries
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustHaveAccessToSubdomainRequirement { SubdomainId = _otherSubdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { _domainId }, // User moderates _domainId
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateMustHaveAccessToSubdomainHandler(httpContextAccessorMock.Object, _fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Assert
        result.IsAuthorized.Should().BeFalse(
            "cross-domain access must be denied - user moderates different domain (tenant isolation)");
    }

    private static IAuthorizationHandler<MustBeModeratorToSubdomainRequirement> CreateMustBeModeratorToSubdomainHandler(
        IHttpContextAccessor httpContextAccessor,
        IDocumentSession documentSession)
    {
        var requirementType = typeof(MustBeModeratorToSubdomainRequirement);
        var handlerType = requirementType.GetNestedType("MustBeModeratorToSubdomainRequirementHandler", BindingFlags.NonPublic);
        return (IAuthorizationHandler<MustBeModeratorToSubdomainRequirement>)Activator.CreateInstance(handlerType!, httpContextAccessor, documentSession)!;
    }

    private static IAuthorizationHandler<MustHaveAccessToSubdomainRequirement> CreateMustHaveAccessToSubdomainHandler(
        IHttpContextAccessor httpContextAccessor,
        IDocumentSession documentSession)
    {
        var requirementType = typeof(MustHaveAccessToSubdomainRequirement);
        var handlerType = requirementType.GetNestedType("MustHaveAccessToSubdomainRequirementHandler", BindingFlags.NonPublic);
        return (IAuthorizationHandler<MustHaveAccessToSubdomainRequirement>)Activator.CreateInstance(handlerType!, httpContextAccessor, documentSession)!;
    }

    private static HttpContext CreateHttpContextWithUserAuthInfo(UserAuthInfo userAuthInfo)
    {
        var httpContext = new DefaultHttpContext();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userAuthInfo.UserId ?? string.Empty),
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

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        return httpContext;
    }
}
