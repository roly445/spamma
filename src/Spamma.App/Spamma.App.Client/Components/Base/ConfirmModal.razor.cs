using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components.Base;

/// <summary>
/// Code-behind for the confirm modal wrapper component.
/// </summary>
public partial class ConfirmModal
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnConfirm { get; set; }

    [Parameter]
    public bool AllowBackdropClose { get; set; } = false; // default for destructive actions

    [Parameter]
    public string WidthClass { get; set; } = "sm:max-w-md";

    [Parameter]
    public string AriaLabelledBy { get; set; } = "confirm-modal-title";

    [Parameter]
    public string Title { get; set; } = "Confirm";

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public string ConfirmLabel { get; set; } = "Confirm";

    [Parameter]
    public string CancelLabel { get; set; } = "Cancel";

    [Parameter]
    public string Variant { get; set; } = "danger"; // danger, warning, default

    [Parameter]
    public bool IsProcessing { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string IconBgClass => Variant switch
    {
        "danger" => "bg-red-600",
    "warning" => "bg-yellow-500",
    "success" => "bg-green-600",
        _ => "bg-blue-600",
    };

    private string ConfirmButtonClasses => Variant switch
    {
        "danger" => "bg-red-600 hover:bg-red-700 text-white",
    "warning" => "bg-yellow-500 hover:bg-yellow-600 text-white",
    "success" => "bg-green-600 hover:bg-green-700 text-white",
        _ => "bg-blue-600 hover:bg-blue-700 text-white",
    };

    private async Task Confirm()
    {
        if (OnConfirm.HasDelegate)
        {
            await OnConfirm.InvokeAsync();
        }
        else
        {
            await OnClose.InvokeAsync();
        }
    }
}
