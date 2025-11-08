using FluentAssertions;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToCampaignRequirementHandlerTests
{
    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsFail()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var requirement = new MustHaveAccessToCampaignRequirement { CampaignId = campaignId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampaignSummary?)null);

        var httpContextMock = new Mock<HttpContext>(MockBehavior.Strict);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserIsDomainAdmin_ReturnsSucceed()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var campaign = new CampaignSummary { CampaignId = campaignId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToCampaignRequirement { CampaignId = campaignId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "Admin",
            "admin@example.com",
            SystemRole.DomainManagement,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextMockWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesSubdomain_ReturnsSucceed()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var campaign = new CampaignSummary { CampaignId = campaignId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToCampaignRequirement { CampaignId = campaignId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            new[] { subdomainId },
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextMockWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserCanViewSubdomain_ReturnsSucceed()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var campaign = new CampaignSummary { CampaignId = campaignId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToCampaignRequirement { CampaignId = campaignId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { subdomainId });

        var httpContextMock = CreateHttpContextMockWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesDomain_ReturnsSucceed()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var campaign = new CampaignSummary { CampaignId = campaignId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToCampaignRequirement { CampaignId = campaignId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextMockWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserHasNoAccess_ReturnsFail()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var campaign = new CampaignSummary { CampaignId = campaignId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToCampaignRequirement { CampaignId = campaignId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<CampaignSummary>(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextMock = CreateHttpContextMockWithUserAuthInfo(userAuthInfo);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock);

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessorMock.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeFalse();
    }

    private static IAuthorizationHandler<MustHaveAccessToCampaignRequirement> CreateHandler(
        IDocumentSession documentSession,
        IHttpContextAccessor httpContextAccessor)
    {
        var requirementType = typeof(MustHaveAccessToCampaignRequirement);
        var handlerType = requirementType.GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .First(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IAuthorizationHandler<>) &&
                i.GetGenericArguments()[0] == requirementType));

        var handler = Activator.CreateInstance(handlerType, httpContextAccessor, documentSession);
        return (IAuthorizationHandler<MustHaveAccessToCampaignRequirement>)handler!;
    }

    private static HttpContext CreateHttpContextMockWithUserAuthInfo(UserAuthInfo userAuthInfo)
    {
        // Create a real DefaultHttpContext that will execute the extension method properly
        var httpContext = new DefaultHttpContext();

        // Add claims for UserAuthInfo to work correctly
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userAuthInfo.UserId ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Name, userAuthInfo.Name ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Email, userAuthInfo.EmailAddress ?? string.Empty),
            new(System.Security.Claims.ClaimTypes.Role, userAuthInfo.SystemRole.ToString()),
        };

        // Add moderated domain claims
        foreach (var domainId in userAuthInfo.ModeratedDomains)
        {
            claims.Add(new System.Security.Claims.Claim("moderated_domain", domainId.ToString()));
        }

        // Add moderated subdomain claims
        foreach (var subdomainId in userAuthInfo.ModeratedSubdomains)
        {
            claims.Add(new System.Security.Claims.Claim("moderated_subdomain", subdomainId.ToString()));
        }

        // Add viewable subdomain claims
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
