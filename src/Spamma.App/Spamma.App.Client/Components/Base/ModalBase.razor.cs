using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components.Base;

/// <summary>
/// Code-behind for the modal base component.
/// </summary>
public partial class ModalBase
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public bool AllowBackdropClose { get; set; }

    [Parameter]
    public string WidthClass { get; set; } = "sm:max-w-sm w-full";

    [Parameter]
    public string? AriaLabelledBy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private async Task HandleBackdropClick()
    {
        if (AllowBackdropClose)
        {
            await OnClose.InvokeAsync();
        }
    }
}