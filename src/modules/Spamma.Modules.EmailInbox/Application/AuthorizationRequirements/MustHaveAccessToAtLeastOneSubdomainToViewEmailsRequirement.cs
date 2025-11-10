using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

public class MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirementHandler(
        IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement>
    {
        public Task<AuthorizationResult> Handle(
            MustHaveAccessToAtLeastOneSubdomainToViewEmailsRequirement requirement,
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