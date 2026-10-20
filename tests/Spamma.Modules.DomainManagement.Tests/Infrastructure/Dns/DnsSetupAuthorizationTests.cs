using System.Security.Claims;
using Marten;
using Microsoft.AspNetCore.Http;
using Moq;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Infrastructure.Dns;

public class DnsSetupAuthorizationTests
{
    [Fact]
    public async Task DomainSetup_RequiresDomainManagementOrModerator()
    {
        var domainId = Guid.NewGuid();
        var admin = new SetupDomainDnsCommandAuthorizer(CreateAccessor([new(ClaimTypes.Role, SystemRole.DomainManagement.ToString())]));
        var moderator = new SetupDomainDnsCommandAuthorizer(CreateAccessor([new("moderated_domain", domainId.ToString())]));
        var unrelated = new SetupDomainDnsCommandAuthorizer(CreateAccessor([]));

        Assert.True((await admin.Authorize(new(domainId), CancellationToken.None)).IsAuthorized);
        Assert.True((await moderator.Authorize(new(domainId), CancellationToken.None)).IsAuthorized);
        Assert.False((await unrelated.Authorize(new(domainId), CancellationToken.None)).IsAuthorized);
    }

    [Fact]
    public async Task SubdomainSetup_ViewerCannotWrite()
    {
        var subdomainId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        var viewer = new SetupSubdomainDnsCommandAuthorizer(CreateAccessor([new("viewable_subdomain", subdomainId.ToString())]), session.Object);
        var moderator = new SetupSubdomainDnsCommandAuthorizer(CreateAccessor([new("moderated_subdomain", subdomainId.ToString())]), session.Object);

        Assert.False((await viewer.Authorize(new(subdomainId), CancellationToken.None)).IsAuthorized);
        Assert.True((await moderator.Authorize(new(subdomainId), CancellationToken.None)).IsAuthorized);
    }

    private static IHttpContextAccessor CreateAccessor(IEnumerable<Claim> additionalClaims)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        claims.AddRange(additionalClaims);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(context);
        return accessor.Object;
    }
}
