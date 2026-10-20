using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;

internal class SetupSubdomainDnsCommandAuthorizer(IHttpContextAccessor accessor, IDocumentSession session) : IBluQubeAuthorizer<SetupSubdomainDnsCommand>
{
    public async Task<AuthorizationResult> Authorize(SetupSubdomainDnsCommand request, CancellationToken cancellationToken)
    {
        var user = accessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedSubdomains.Contains(request.SubdomainId))
        {
            return AuthorizationResult.Succeed();
        }

        var subdomain = await session.LoadAsync<SubdomainLookup>(request.SubdomainId, cancellationToken);
        return subdomain is not null && user.ModeratedDomains.Contains(subdomain.DomainId)
            ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
    }
}
