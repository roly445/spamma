using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Code-behind for the Welcome component.
/// </summary>
public partial class Welcome
{
    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    protected override void OnInitialized()
    {
        this.Layout.CurrentStep = "0";
    }
}