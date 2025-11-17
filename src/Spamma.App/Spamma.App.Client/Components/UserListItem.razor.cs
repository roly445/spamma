using System.Text;
using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components;

/// <summary>
/// Code-behind for the UserListItem component.
/// </summary>
public partial class UserListItem
{
    private string _userInitials = "U";
    private string? _gravatarUrl;

    [Parameter]
    public string? UserName { get; set; }

    [Parameter]
    public string UserEmail { get; set; } = string.Empty;

    [Parameter]
    public bool ShowName { get; set; } = false;

    [Parameter]
    public string Size { get; set; } = "10";

    protected override void OnInitialized()
    {
        // Calculate user initials
        if (!string.IsNullOrEmpty(this.UserName))
        {
            var parts = this.UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this._userInitials = parts.Length > 1
                ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}"
                : $"{char.ToUpper(parts[0][0])}";
        }
        else if (!string.IsNullOrEmpty(this.UserEmail))
        {
            this._userInitials = this.UserEmail[0].ToString().ToUpper();
        }

        // Generate Gravatar URL
        if (!string.IsNullOrEmpty(this.UserEmail))
        {
            var hash = ComputeSimpleHash(this.UserEmail);
            var sizeParam = this.Size switch
            {
                "10" => 40,
                "8" => 32,
                _ => 40,
            };
            this._gravatarUrl = $"https://www.gravatar.com/avatar/{hash}?s={sizeParam}&d=404";
        }
    }

    private static string ComputeSimpleHash(string email)
    {
        var emailLower = email.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(emailLower);
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

    private string GetInitialsFontSize() => this.Size switch
    {
        "8" => "text-xs",
        "10" => "text-sm",
        "12" => "text-base",
        "16" => "text-lg",
        "20" => "text-xl",
        _ => "text-sm",
    };
}