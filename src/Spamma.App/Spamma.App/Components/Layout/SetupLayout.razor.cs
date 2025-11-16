namespace Spamma.App.Components.Layout;

public partial class SetupLayout
{
    public string CurrentStep { get; set; } = "0";

    public List<string> CompletedSteps { get; set; } = new();
}