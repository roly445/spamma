using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class ListDnsIntegrationsQueryAuthorizer(IHttpContextAccessor accessor) : IBluQubeAuthorizer<ListDnsIntegrationsQuery>
{
    public Task<AuthorizationResult> Authorize(ListDnsIntegrationsQuery request, CancellationToken cancellationToken)
    {
        var user = accessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated && user.SystemRole.HasFlag(SystemRole.DomainManagement)
            ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}
