using FluentAssertions;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

namespace Spamma.Modules.DomainManagement.Tests.Application.AuthorizationRequirements;

public class MustBeModeratorToDomainRequirementHandlerTests
{
    [Fact]
    public async Task Handle_UserIsDomainAdministrator_ReturnsSucceed()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var requirement = new MustBeModeratorToDomainRequirement { DomainId = domainId };

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

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesTargetDomain_ReturnsSucceed()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToDomainRequirement { DomainId = domainId };

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

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesOtherDomain_ReturnsFail()
    {
        // Arrange
        var targetDomainId = Guid.NewGuid();
        var userDomainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToDomainRequirement { DomainId = targetDomainId };

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { userDomainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserHasNoRolesOrClaims_ReturnsFail()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToDomainRequirement { DomainId = domainId };

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

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserHasSubdomainClaimButNotDomainClaim_ReturnsFail()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var requirement = new MustBeModeratorToDomainRequirement { DomainId = domainId };

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

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    private static IAuthorizationHandler<MustBeModeratorToDomainRequirement> CreateHandler(
        IHttpContextAccessor httpContextAccessor)
    {
        var requirementType = typeof(MustBeModeratorToDomainRequirement);
        var handlerType = requirementType.GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .First(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IAuthorizationHandler<>) &&
                i.GetGenericArguments()[0] == requirementType));

        var handler = Activator.CreateInstance(handlerType, httpContextAccessor);
        return (IAuthorizationHandler<MustBeModeratorToDomainRequirement>)handler!;
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
