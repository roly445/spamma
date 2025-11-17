using FluentAssertions;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Tests.Integration.AuthorizationRequirements;

public class MustBeModeratorToSubdomainRequirementIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private Guid _domainId;
    private Guid _subdomainId;

    public MustBeModeratorToSubdomainRequirementIntegrationTests(PostgreSqlFixture fixture)
    {
        this._fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Seed test data: create a domain and subdomain
        this._domainId = Guid.NewGuid();
        this._subdomainId = Guid.NewGuid();

        // Create subdomain lookup for authorization queries
        var subdomainLookup = new SubdomainLookup
        {
            Id = this._subdomainId,
            DomainId = this._domainId,
            SubdomainName = "test-subdomain",
            CreatedAt = DateTime.UtcNow,
            IsSuspended = false,
            FullName = "test-subdomain.example.com",
            ParentName = "example.com",
        };

        this._fixture.Session!.Store(subdomainLookup);
        await this._fixture.Session.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_UserModeratesParentDomain_ReturnsSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = this._subdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { this._domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(httpContextAccessorMock.Object, this._fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesOtherSubdomain_ReturnsFail()
    {
        // Arrange
        var targetSubdomainId = this._subdomainId;
        var userSubdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = targetSubdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            new[] { userSubdomainId },
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(httpContextAccessorMock.Object, this._fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserHasNoRolesOrClaims_ReturnsFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = this._subdomainId };

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

        var handler = CreateHandler(httpContextAccessorMock.Object, this._fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserHasViewableSubdomainClaimButNotModerator_ReturnsFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = this._subdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { this._subdomainId });

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(httpContextAccessorMock.Object, this._fixture.Session!);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    private static IAuthorizationHandler<MustBeModeratorToSubdomainRequirement> CreateHandler(
        IHttpContextAccessor httpContextAccessor,
        IDocumentSession documentSession)
    {
        var requirementType = typeof(MustBeModeratorToSubdomainRequirement);
        var handlerType = requirementType.GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .First(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IAuthorizationHandler<>) &&
                i.GetGenericArguments()[0] == requirementType));

        var handler = Activator.CreateInstance(handlerType, httpContextAccessor, documentSession);
        return (IAuthorizationHandler<MustBeModeratorToSubdomainRequirement>)handler!;
    }

    private static HttpContext CreateHttpContextWithUserAuthInfo(UserAuthInfo userAuthInfo)
    {
        var httpContext = new DefaultHttpContext();

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userAuthInfo!.UserId.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, userAuthInfo.Name ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Email, userAuthInfo.EmailAddress ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Role, userAuthInfo.SystemRole.ToString()),
        };

        foreach (var domainId in userAuthInfo.ModeratedDomains)
        {
            claims.Add(new System.Security.Claims.Claim("moderated_domain", domainId.ToString()));
        }

        foreach (var subdomainId in userAuthInfo.ModeratedSubdomains)
        {
            claims.Add(new System.Security.Claims.Claim("moderated_subdomain", subdomainId.ToString()));
        }

        foreach (var subdomainId in userAuthInfo.ViewableSubdomains)
        {
            claims.Add(new System.Security.Claims.Claim("viewable_subdomain", subdomainId.ToString()));
        }

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        httpContext.User = principal;

        return httpContext;
    }
}
