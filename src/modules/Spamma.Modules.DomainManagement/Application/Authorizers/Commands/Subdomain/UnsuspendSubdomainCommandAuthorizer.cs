using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;

internal class UnsuspendSubdomainCommandAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<UnsuspendSubdomainCommand>
{
    public async Task<AuthorizationResult> Authorize(UnsuspendSubdomainCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedSubdomains.Contains(request.SubdomainId))
        {
            return AuthorizationResult.Succeed();
        }

        var hasDomainAccess = await documentSession.Query<SubdomainLookup>()
            .AnyAsync(x => x.Id == request.SubdomainId && user.ModeratedDomains.Contains(x.DomainId), token: cancellationToken);

        return hasDomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
    }
}

