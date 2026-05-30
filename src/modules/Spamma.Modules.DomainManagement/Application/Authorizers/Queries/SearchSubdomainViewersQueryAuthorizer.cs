using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class SearchSubdomainViewersQueryAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<SearchSubdomainViewersQuery>
{
    public Task<AuthorizationResult> Authorize(SearchSubdomainViewersQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedDomains.Any() || user.ModeratedSubdomains.Any())
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }

        return Task.FromResult(AuthorizationResult.Fail());
    }
}

