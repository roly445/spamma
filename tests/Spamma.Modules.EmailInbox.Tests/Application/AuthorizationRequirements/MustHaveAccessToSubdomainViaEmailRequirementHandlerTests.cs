using FluentAssertions;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Application.AuthorizationRequirements;

public class MustHaveAccessToSubdomainViaEmailRequirementHandlerTests
{
    [Fact]
    public async Task Handle_EmailNotFound_ReturnsFail()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement { EmailId = emailId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<EmailLookup>(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailLookup?)null);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(() => new Microsoft.AspNetCore.Http.DefaultHttpContext());

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
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var email = new EmailLookup { Id = emailId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement { EmailId = emailId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<EmailLookup>(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        var userAuthInfo = UserAuthInfo.Authenticated(
            Guid.NewGuid().ToString(),
            "Admin",
            "admin@example.com",
            SystemRole.DomainManagement,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessor.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessor.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesDomain_ReturnsSucceed()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = new EmailLookup { Id = emailId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement { EmailId = emailId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<EmailLookup>(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            new[] { domainId },
            Array.Empty<Guid>(),
            Array.Empty<Guid>());

        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessor.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessor.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserModeratesSubdomain_ReturnsSucceed()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = new EmailLookup { Id = emailId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement { EmailId = emailId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<EmailLookup>(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            new[] { subdomainId },
            Array.Empty<Guid>());

        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessor.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessor.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserCanViewSubdomain_ReturnsSucceed()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var email = new EmailLookup { Id = emailId, SubdomainId = subdomainId, DomainId = domainId };
        var requirement = new MustHaveAccessToSubdomainViaEmailRequirement { EmailId = emailId };

        var documentSessionMock = new Mock<IDocumentSession>(MockBehavior.Strict);
        documentSessionMock
            .Setup(x => x.LoadAsync<EmailLookup>(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        var userAuthInfo = UserAuthInfo.Authenticated(
            userId,
            "User",
            "user@example.com",
            0,
            Array.Empty<Guid>(),
            Array.Empty<Guid>(),
            new[] { subdomainId });

        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessor.Setup(x => x.HttpContext).Returns(CreateHttpContextWithUserAuthInfo(userAuthInfo));

        var handler = CreateHandler(documentSessionMock.Object, httpContextAccessor.Object);

        // Act
        var result = await handler.Handle(requirement, CancellationToken.None);

        // Verify
        result.IsAuthorized.Should().BeTrue();
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

    private static IAuthorizationHandler<MustHaveAccessToSubdomainViaEmailRequirement> CreateHandler(
        IDocumentSession documentSession,
        IHttpContextAccessor httpContextAccessor)
    {
        var requirementType = typeof(MustHaveAccessToSubdomainViaEmailRequirement);
        var handlerType = requirementType.GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .First(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IAuthorizationHandler<>) &&
                i.GetGenericArguments()[0] == requirementType));

        var handler = Activator.CreateInstance(handlerType, httpContextAccessor, documentSession);
        return (IAuthorizationHandler<MustHaveAccessToSubdomainViaEmailRequirement>)handler!;
    }
}