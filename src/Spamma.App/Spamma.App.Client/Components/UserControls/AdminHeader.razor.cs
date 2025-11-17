using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components.UserControls;

/// <summary>
/// Code-behind for the admin header component.
/// </summary>
public partial class AdminHeader : ComponentBase
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Subtitle { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? Controls { get; set; }
}