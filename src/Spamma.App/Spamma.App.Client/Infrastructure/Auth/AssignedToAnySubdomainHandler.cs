using Microsoft.AspNetCore.Authorization;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public class AssignedToAnySubdomainHandler : AuthorizationHandler<AssignedToAnySubdomainRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AssignedToAnySubdomainRequirement requirement)
    {
        var userInfo = context.ToUserAuthInfo();
        if (!userInfo.IsAuthenticated)
        {
            return Task.CompletedTask;
        }
        
        if (userInfo.SystemRole.HasFlag(SystemRole.DomainManagement))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (userInfo.ModeratedDomains.Any())
        {
            context.Succeed(requirement);
        }
        

        if (userInfo.ModeratedSubdomains.Any())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}