using FluentAssertions;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

namespace Spamma.Modules.DomainManagement.Tests.Application.AuthorizationRequirements;

public class MustBeModeratorToSubdomainRequirementHandlerTests
{
    [Fact]
    public async Task Handle_UserIsDomainAdministrator_ReturnsSucceed()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = subdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "Admin",
            "admin@example.com",
            SystemRole.DomainManagement,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);

        var handler = CreateHandler(httpContextAccessorMock.Object, documentSessionMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesTargetSubdomain_ReturnsSucceed()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = subdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            new[] { subdomainId },
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);

        var handler = CreateHandler(httpContextAccessorMock.Object, documentSessionMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact(Skip = "Requires database query - covered by integration test MustBeModeratorToSubdomainRequirementIntegrationTests.Handle_UserModeratesParentDomain_ReturnsSucceed")]
    public async Task Handle_UserModeratesParentDomain_RequiresDatabaseQuery()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = subdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);

        var handler = CreateHandler(httpContextAccessorMock.Object, documentSessionMock.Object);

        // Act & Verify
        // Note: This test verifies that when user has domain moderator claim but NOT subdomain claim,
        // the handler attempts to query the database via documentSession.Query<SubdomainLookup>().AnyAsync(...)
        // Since we can't easily mock Marten's IQueryable extension methods without creating integration tests,
        // we accept that this will throw due to unmocked Query() call.
        // This behavior is expected and documents the database lookup path in the authorization flow.
        await FluentActions.Invoking(async () => await handler.Handle(requirement, CancellationToken.None))
            .Should().ThrowAsync<Exception>();
    }

    [Fact(Skip = "Requires database query - covered by integration test MustBeModeratorToSubdomainRequirementIntegrationTests.Handle_UserModeratesOtherSubdomain_ReturnsFail")]
    public async Task Handle_UserModeratesOtherSubdomain_ReturnsFail()
    {
        // Arrange
        var targetSubdomainId = Guid.NewGuid();
        var userSubdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
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

        // Use Loose mocking since we can't mock Marten IQueryable easily
        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Loose);

        var handler = CreateHandler(httpContextAccessorMock.Object, documentSessionMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact(Skip = "Requires database query - covered by integration test MustBeModeratorToSubdomainRequirementIntegrationTests.Handle_UserHasNoRolesOrClaims_ReturnsFail")]
    public async Task Handle_UserHasNoRolesOrClaims_ReturnsFail()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = subdomainId };

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

        // Use Loose mocking since we can't mock Marten IQueryable easily
        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Loose);

        var handler = CreateHandler(httpContextAccessorMock.Object, documentSessionMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact(Skip = "Requires database query - covered by integration test MustBeModeratorToSubdomainRequirementIntegrationTests.Handle_UserHasViewableSubdomainClaimButNotModerator_ReturnsFail")]
    public async Task Handle_UserHasViewableSubdomainClaimButNotModerator_ReturnsFail()
    {
        // Arrange
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToSubdomainRequirement { SubdomainId = subdomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { subdomainId });

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        // Use Loose mocking since we can't mock Marten IQueryable easily
        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Loose);

        var handler = CreateHandler(httpContextAccessorMock.Object, documentSessionMock.Object);

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
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userAuthInfo.UserId ?? string.Empty),
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