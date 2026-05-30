using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetCampaignsQueryAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<GetCampaignsQuery>
{
    public Task<AuthorizationResult> Authorize(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
            user.ModeratedSubdomains.Any() ||
            user.ViewableSubdomains.Any() ||
            user.ModeratedDomains.Any())
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }

        return Task.FromResult(AuthorizationResult.Fail());
    }
}
