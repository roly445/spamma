using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeModeratorToAtLeastOneSubdomainRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeModeratorToAtLeastOneSubdomainRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeModeratorToAtLeastOneSubdomainRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeModeratorToAtLeastOneSubdomainRequirement request, CancellationToken cancellationToken = default)
        {
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
}