using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers;

internal static class AuthorizerTestContext
{
    internal static IHttpContextAccessor CreateAuthenticated(SystemRole systemRole = 0)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com"),
        };

        if (systemRole != 0)
        {
            claims.Add(new Claim(ClaimTypes.Role, systemRole.ToString()));
        }

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
            },
        };
    }

    internal static IHttpContextAccessor CreateUnauthenticated()
    {
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
            },
        };
    }
}
