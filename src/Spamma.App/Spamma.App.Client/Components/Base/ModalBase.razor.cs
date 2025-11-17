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
    public string WidthClass { get; set; } = ModalSizes.Small;

    [Parameter]
    public string? AriaLabelledBy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private async Task HandleBackdropClick()
    {
        if (this.AllowBackdropClose)
        {
            await this.OnClose.InvokeAsync();
        }
    }

    public static class ModalSizes
    {
        public const string Small = "sm:max-w-sm w-full";

        public const string Medium = "sm:max-w-md w-full";

        public const string Large = "sm:max-w-lg w-full";

        public const string ExtraLarge = "sm:max-w-2xl w-full";
    }
}