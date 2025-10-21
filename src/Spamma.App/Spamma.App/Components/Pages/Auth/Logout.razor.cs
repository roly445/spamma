using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Spamma.App.Components.Pages.Auth;

/// <summary>
/// Backing code for the logout page.
/// </summary>
public partial class Logout(IHttpContextAccessor httpContextAccessor, ILogger<Logout> logger)
{
    protected override async Task OnInitializedAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            logger.LogInformation("Logging out user: {UserId}", httpContext.User.FindFirst("user_id")?.Value ?? "Unknown");

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            logger.LogInformation("User successfully logged out");
        }
    }
}