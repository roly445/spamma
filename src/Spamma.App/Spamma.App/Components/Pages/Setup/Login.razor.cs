using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Spamma.App.Infrastructure;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Backing logic for the setup login page.
/// </summary>
public partial class Login(IInMemorySetupAuthService setupAuth, IHttpContextAccessor httpContextAccessor, NavigationManager navigation, ILogger<Login> logger)
{
    private string errorMessage = string.Empty;
    private bool showPasswordHint = true;

    [SupplyParameterFromForm(FormName = "SetupLoginForm")]
    private LoginModel Model { get; set; } = new();

    protected override void OnInitialized()
    {
        this.showPasswordHint = httpContextAccessor.HttpContext.IsLocal();
    }

    private void HandleLogin()
    {
        this.errorMessage = string.Empty;

        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            var isValid = setupAuth.ValidatePassword(this.Model.Password, ipAddress, userAgent);

            if (isValid)
            {
                // Set session authentication
                httpContext!.Session.SetString("SetupAuthenticated", "true");

                logger.LogInformation("Setup authentication successful from IP: {IpAddress}", ipAddress);

                // Simple redirect - let the middleware handle the rest
                navigation.NavigateTo("/setup", true);
            }
            else
            {
                this.errorMessage = "Invalid setup password. Check the application logs for the correct password.";
            }
        }
        catch (Exception ex) when (ex is not NavigationException)
        {
            this.errorMessage = "Authentication error. Please try again.";
            logger.LogError(ex, "Error during setup authentication");
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Setup password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}