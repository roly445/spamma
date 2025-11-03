using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public static class AuthenticationStateExtensions
{
    public static UserAuthInfo ToUserAuthInfo(this AuthenticationState authState)
    {
        if (authState.User?.Identity?.IsAuthenticated != true)
        {
            return UserAuthInfo.Unauthenticated();
        }

        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = authState.User.FindFirst(ClaimTypes.Name)?.Value;
        var email = authState.User.FindFirst(ClaimTypes.Email)?.Value;

        var roleClaimValue = authState.User.FindFirst(ClaimTypes.Role)?.Value;
        var systemRole = Enum.TryParse<SystemRole>(roleClaimValue, out var role) ? role : 0;

        var moderatedDomains = authState.User.FindAll(Lookups.ModeratedDomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var moderatedSubdomains = authState.User.FindAll(Lookups.ModeratedSubdomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var viewableSubdomains = authState.User.FindAll(Lookups.ViewableSubdomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        return UserAuthInfo.Authenticated(
            userId ?? string.Empty,
            name ?? string.Empty,
            email ?? string.Empty,
            systemRole,
            moderatedDomains,
            moderatedSubdomains,
            viewableSubdomains);
    }
    
    public static UserAuthInfo ToUserAuthInfo(this AuthorizationHandlerContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return UserAuthInfo.Unauthenticated();
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = context.User.FindFirst(ClaimTypes.Name)?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;

        var roleClaimValue = context.User.FindFirst(ClaimTypes.Role)?.Value;
        var systemRole = Enum.TryParse<SystemRole>(roleClaimValue, out var role) ? role : 0;

        var moderatedDomains = context.User.FindAll(Lookups.ModeratedDomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var moderatedSubdomains = context.User.FindAll(Lookups.ModeratedSubdomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var viewableSubdomains = context.User.FindAll(Lookups.ViewableSubdomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        return UserAuthInfo.Authenticated(
            userId ?? string.Empty,
            name ?? string.Empty,
            email ?? string.Empty,
            systemRole,
            moderatedDomains,
            moderatedSubdomains,
            viewableSubdomains);
    }
}


