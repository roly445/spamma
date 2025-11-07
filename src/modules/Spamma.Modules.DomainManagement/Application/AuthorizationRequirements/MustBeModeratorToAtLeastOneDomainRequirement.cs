using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeModeratorToAtLeastOneDomainRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeModeratorToAtLeastOneDomainRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeModeratorToAtLeastOneDomainRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeModeratorToAtLeastOneDomainRequirement request, CancellationToken cancellationToken = default)
        {
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (!user.IsAuthenticated)
            {
                return Task.FromResult(AuthorizationResult.Fail());
            }

            if (user.SystemRole.HasFlag(SystemRole.DomainManagement))
            {
                return Task.FromResult(AuthorizationResult.Succeed());
            }

            var hasModeratedDomains = user.ModeratedDomains.Any();
            return Task.FromResult(hasModeratedDomains ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}