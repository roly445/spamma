using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<GetChaosAddressBySubdomainAndLocalPartQuery>
{
    public async Task<AuthorizationResult> Authorize(GetChaosAddressBySubdomainAndLocalPartQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
            user.ModeratedSubdomains.Contains(request.SubdomainId) ||
            user.ViewableSubdomains.Contains(request.SubdomainId))
        {
            return AuthorizationResult.Succeed();
        }

        var hasSubdomainAccess = await documentSession.Query<SubdomainLookup>()
            .AnyAsync(x => x.Id == request.SubdomainId && user.ModeratedDomains.Contains(x.DomainId), token: cancellationToken);

        return hasSubdomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
    }
}

