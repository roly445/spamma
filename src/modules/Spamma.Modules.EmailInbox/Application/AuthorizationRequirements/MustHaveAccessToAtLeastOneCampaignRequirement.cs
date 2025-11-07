using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

public class MustHaveAccessToAtLeastOneCampaignRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustHaveAccessToAtLeastOneCampaignRequirementHandler(
        IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustHaveAccessToAtLeastOneCampaignRequirement>
    {
        public Task<AuthorizationResult> Handle(
            MustHaveAccessToAtLeastOneCampaignRequirement requirement,
            CancellationToken cancellationToken = default)
        {
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
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
}