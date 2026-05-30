using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class SearchSubdomainsQueryAuthorizer(IInternalQueryStore internalQueryStore, IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<SearchSubdomainsQuery>
{
    public Task<AuthorizationResult> Authorize(SearchSubdomainsQuery request, CancellationToken cancellationToken)
    {
        if (internalQueryStore.IsQueryStored(request))
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }

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

