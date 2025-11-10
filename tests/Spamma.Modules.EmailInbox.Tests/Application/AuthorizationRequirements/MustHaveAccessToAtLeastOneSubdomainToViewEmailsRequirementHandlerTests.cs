using FluentAssertions;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirementHandlerTests
{
    [Fact]
    public Task Handle_UserIsDomainAdmin_ReturnsSucceed()
    {
        // Arrange
        var requirement = new MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement();

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "Admin",
            "admin@example.com",
            SystemRole.DomainManagement,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.Result.IsAuthorized.Should().BeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_UserModeratesSubdomain_ReturnsSucceed()
    {
        // Arrange
        var requirement = new MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement();
        var subdomainId = Guid.NewGuid();

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            new[] { subdomainId },
            Array.Empty<Guid>());

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.Result.IsAuthorized.Should().BeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_UserCanViewSubdomain_ReturnsSucceed()
    {
        // Arrange
        var requirement = new MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement();
        var subdomainId = Guid.NewGuid();

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { subdomainId });

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.Result.IsAuthorized.Should().BeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_UserHasNoAccess_ReturnsFail()
    {
        // Arrange
        var requirement = new MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement();

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(httpContextAccessorMock.Object);

        // Act
        var result = handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.Result.IsAuthorized.Should().BeFalse();
        return Task.CompletedTask;
    }

    private static Microsoft.AspNetCore.Http.HttpContext CreateHttpContextWithUserAuthInfo(UserAuthInfo userAuthInfo)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userAuthInfo.UserId ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Name, userAuthInfo.Name ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Email, userAuthInfo.EmailAddress ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Role, userAuthInfo.SystemRole.ToString()),
        };

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

    private static IAuthorizationHandler<MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement> CreateHandler(IHttpContextAccessor httpContextAccessor)
    {
        var requirementType = typeof(MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement);
        var handlerType = requirementType.GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .First(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IAuthorizationHandler<>) &&
                i.GetGenericArguments()[0] == requirementType));

        var handler = Activator.CreateInstance(handlerType, httpContextAccessor);
        return (IAuthorizationHandler<MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement>)handler!;
    }
}
