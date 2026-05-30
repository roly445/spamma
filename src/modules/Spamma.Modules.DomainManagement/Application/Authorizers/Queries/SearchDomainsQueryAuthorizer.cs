using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class SearchDomainsQueryAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<SearchDomainsQuery>
{
    public Task<AuthorizationResult> Authorize(SearchDomainsQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement))
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }

        return Task.FromResult(user.ModeratedDomains.Any() ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

