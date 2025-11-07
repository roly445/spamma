using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;

namespace Spamma.Modules.Common;

public static class HttpContextExtensions
{
    public static UserAuthInfo ToUserAuthInfo(this HttpContext? httpContext)
    {
        if (httpContext == null || httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return UserAuthInfo.Unauthenticated();
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;

        var roleClaimValue = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        var systemRole = Enum.TryParse<SystemRole>(roleClaimValue, out var role) ? role : 0;

        var moderatedDomains = httpContext.User.FindAll(Lookups.ModeratedDomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var moderatedSubdomains = httpContext.User.FindAll(Lookups.ModeratedSubdomainClaim)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var viewableSubdomains = httpContext.User.FindAll(Lookups.ViewableSubdomainClaim)
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