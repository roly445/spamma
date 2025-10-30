using System.Security.Claims;
using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeModeratorToAtLeastOneDomainRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeModeratorToAtLeastOneDomainRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeModeratorToAtLeastOneDomainRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeModeratorToAtLeastOneDomainRequirement request, CancellationToken cancellationToken = default)
        {
             var context = httpContextAccessor.HttpContext;
             var claim = context!.User.FindFirst(ClaimTypes.Role)?.Value;
             if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement))
             {
                  return Task.FromResult(AuthorizationResult.Succeed());
             }

             return Task.FromResult(context.User.HasClaim(c => c.Type == Lookups.ModeratedDomainClaim) ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}