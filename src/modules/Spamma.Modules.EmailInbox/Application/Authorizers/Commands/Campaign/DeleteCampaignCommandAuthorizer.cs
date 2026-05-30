using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

internal class DeleteCampaignCommandAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<DeleteCampaignCommand>
{
    public async Task<AuthorizationResult> Authorize(DeleteCampaignCommand request, CancellationToken cancellationToken)
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
