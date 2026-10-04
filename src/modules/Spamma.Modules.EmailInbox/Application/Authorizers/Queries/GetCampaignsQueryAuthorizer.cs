using BluQube.Authorization;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetCampaignsQueryAuthorizer(IHttpContextAccessor httpContextAccessor, IQueryRunner queryRunner) : IBluQubeAuthorizer<GetCampaignsQuery>
{
    public async Task<AuthorizationResult> Authorize(GetCampaignsQuery request, CancellationToken cancellationToken)
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

        if (user.ModeratedDomains.Count == 0)
        {
            return AuthorizationResult.Fail();
        }

        var subdomain = await queryRunner.Send(new GetDetailedSubdomainByIdQuery(request.SubdomainId), cancellationToken);
        return subdomain.Status == QueryResultStatus.Succeeded && user.ModeratedDomains.Contains(subdomain.Data.DomainId)
            ? AuthorizationResult.Succeed()
            : AuthorizationResult.Fail();
    }
}
