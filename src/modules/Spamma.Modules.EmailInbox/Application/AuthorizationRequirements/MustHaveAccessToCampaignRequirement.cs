using JetBrains.Annotations;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

public class MustHaveAccessToCampaignRequirement : IAuthorizationRequirement
{
    public required Guid CampaignId { get; init; }

    [UsedImplicitly]
    private sealed class MustHaveAccessToCampaignRequirementHandler(
        IHttpContextAccessor httpContextAccessor,
        IDocumentSession documentSession)
        : IAuthorizationHandler<MustHaveAccessToCampaignRequirement>
    {
        public async Task<AuthorizationResult> Handle(
            MustHaveAccessToCampaignRequirement requirement,
            CancellationToken cancellationToken = default)
        {
            var campaign = await documentSession.LoadAsync<CampaignSummary>(requirement.CampaignId, cancellationToken);
            if (campaign == null)
            {
                return AuthorizationResult.Fail();
            }

            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
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
}