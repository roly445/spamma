using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Spamma.App.Client.Infrastructure.Constants;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public class AssignedToAnyDomainHandler : AuthorizationHandler<AssignedToAnyDomainRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AssignedToAnyDomainRequirement requirement)
    {
        var claim = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(c => c.Type == Lookups.ModeratedDomainClaim))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}