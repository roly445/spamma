using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public class BitwiseRoleHandler : AuthorizationHandler<BitwiseRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BitwiseRoleRequirement requirement)
    {
        var claim = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(requirement.RequiredRoleValue))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}