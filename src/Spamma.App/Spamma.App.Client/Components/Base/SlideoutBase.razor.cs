using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components.Base;

/// <summary>
/// Backing code for the slideout base component.
/// </summary>
public partial class SlideoutBase
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public string Direction { get; set; } = "right";

    [Parameter]
    public string WidthClass { get; set; } = "max-w-md";

    [Parameter]
    public bool AllowBackdropClose { get; set; } = true;

    [Parameter]
    public string? AriaLabelledBy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private static string SlideInClass => "translate-x-0";

    private string DirectionClass => Direction == "left" ? "left-0" : "right-0";

    private string PaddingClass => Direction == "left" ? "pr-10" : "pl-10";

    private string SlideOutClass => Direction == "left" ? "-translate-x-full" : "translate-x-full";

    private async Task HandleBackdropClick()
    {
        if (AllowBackdropClose)
        {
            await OnClose.InvokeAsync();
        }
    }
}