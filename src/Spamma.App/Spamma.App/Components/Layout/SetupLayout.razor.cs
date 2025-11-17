namespace Spamma.App.Components.Layout;

/// <summary>
/// Code-behind for the setup layout component.
/// </summary>
public partial class SetupLayout
{
    public string CurrentStep { get; set; } = "0";

    public List<string> CompletedSteps { get; set; } = new();
}