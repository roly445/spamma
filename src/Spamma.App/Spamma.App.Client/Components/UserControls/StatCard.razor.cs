using Microsoft.AspNetCore.Components;

namespace Spamma.App.Client.Components.UserControls;

public partial class StatCard : ComponentBase
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public string IconClass { get; set; } = "bg-blue-100";

    [Parameter]
    public RenderFragment ChildContent { get; set; } = null!;
}