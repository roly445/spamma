using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetCampaignDetailQueryAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<GetCampaignDetailQuery>
{
    public async Task<AuthorizationResult> Authorize(GetCampaignDetailQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        var campaign = await documentSession.LoadAsync<CampaignSummary>(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            return AuthorizationResult.Fail();
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
            user.ModeratedSubdomains.Contains(campaign.SubdomainId) ||
            user.ViewableSubdomains.Contains(campaign.SubdomainId) ||
            user.ModeratedDomains.Contains(campaign.DomainId))
        {
            return AuthorizationResult.Succeed();
        }

        return AuthorizationResult.Fail();
    }
}
