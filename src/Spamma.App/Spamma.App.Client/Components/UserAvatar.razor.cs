using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Spamma.App.Client.Infrastructure.Auth;

namespace Spamma.App.Client.Components;

/// <summary>
/// Code-behind for the user avatar component.
/// </summary>
public partial class UserAvatar(AuthenticationStateProvider authenticationStateProvider)
{
    private string? _gravatarUrl;
    private string _userInitials = "U";

    protected override async Task OnInitializedAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.ToUserAuthInfo();

        if (user.IsAuthenticated)
        {
            // Get user initials
            if (!string.IsNullOrEmpty(user.Name))
            {
                var parts = user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                this._userInitials = parts.Length > 1
                    ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}"
                    : $"{char.ToUpper(parts[0][0])}";
            }
            else if (!string.IsNullOrEmpty(user.EmailAddress))
            {
                this._userInitials = user.EmailAddress[0].ToString().ToUpper();
            }

            // Generate Gravatar URL if email exists
            if (!string.IsNullOrEmpty(user.EmailAddress))
            {
                var hash = ComputeSimpleHash(user.EmailAddress);
                this._gravatarUrl = $"https://www.gravatar.com/avatar/{hash}?s=32&d=404";
            }
        }
    }

    private static string ComputeSimpleHash(string email)
    {
        // Simple hash algorithm for Gravatar URLs
        var emailLower = email.Trim().ToLowerInvariant();
        var bytes = System.Text.Encoding.UTF8.GetBytes(emailLower);
        var hash = 0;
        foreach (var b in bytes)
        {
            hash = ((hash << 5) - hash) + b;
        }

        return Math.Abs(hash).ToString("x8");
    }

    private void ShowInitials()
    {
        this._gravatarUrl = null;
        this.StateHasChanged();
    }
}