using System.Security.Claims;
using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeModeratorToAtLeastOneSubdomainRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeModeratorToAtLeastOneSubdomainRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeModeratorToAtLeastOneSubdomainRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeModeratorToAtLeastOneSubdomainRequirement request, CancellationToken cancellationToken = default)
        {
             var context = httpContextAccessor.HttpContext;
             var claim = context!.User.FindFirst(ClaimTypes.Role)?.Value;
             if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement))
             {
                 return Task.FromResult(AuthorizationResult.Succeed());
             }

             if (context.User.HasClaim(c => c.Type == Lookups.ModeratedDomainClaim))
             {
                 return Task.FromResult(AuthorizationResult.Succeed());
             }

             // Check for any assigned domain claim, e.g., "AssignedDomain"
             if (context.User.HasClaim(c => c.Type == Lookups.ModeratedSubdomainClaim))
             {
                 return Task.FromResult(AuthorizationResult.Succeed());
             }

             return Task.FromResult(AuthorizationResult.Fail());
        }
    }
}