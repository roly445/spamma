using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components.UserControls;

/// <summary>
/// Backing code for the domain icon component.
/// </summary>
public partial class DomainIcon
{
    private bool showFallback;

    [Parameter]
    public string DomainName { get; set; } = string.Empty;

    [Parameter]
    public string ContainerClass { get; set; } = "h-10 w-10 bg-gradient-to-br from-indigo-400 to-indigo-600 rounded-lg flex items-center justify-center overflow-hidden";

    [Parameter]
    public string ImageClass { get; set; } = "w-6 h-6 rounded-sm";

    [Parameter]
    public string SvgClass { get; set; } = "h-5 w-5 text-white";

    private string FaviconUrl => $"https://{this.DomainName}/favicon.ico";

    private void OnImageLoad()
    {
        this.showFallback = false;
        this.StateHasChanged();
    }

    private void OnImageError()
    {
        this.showFallback = true;
        this.StateHasChanged();
    }
}