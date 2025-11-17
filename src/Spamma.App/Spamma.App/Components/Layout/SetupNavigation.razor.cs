using Microsoft.AspNetCore.Components;

namespace Spamma.App.Components.Layout;

/// <summary>
/// Code-behind for the setup navigation component.
/// </summary>
public partial class SetupNavigation
{
    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    private string GetStepClasses(string step)
    {
        if (this.Layout.CurrentStep == step)
        {
            return "bg-blue-600 text-white";
        }

        if (this.IsStepCompleted(step))
        {
            return "bg-green-600 text-white";
        }

        if (this.IsStepAccessible(step))
        {
            return "hover:bg-gray-100";
        }

        return "opacity-50";
    }

    private string GetStepIconClasses(string step)
    {
        if (this.Layout.CurrentStep == step)
        {
            return "bg-white text-blue-600";
        }

        if (this.IsStepCompleted(step))
        {
            return "bg-white text-green-600";
        }

        return "bg-gray-400 text-white";
    }

    private bool IsStepCompleted(string step)
    {
        return this.Layout.CompletedSteps.Contains(step);
    }

    private bool IsStepAccessible(string step)
    {
        var stepNum = int.Parse(step);
        var currentStepNum = int.Parse(this.Layout.CurrentStep);
        return stepNum <= currentStepNum + 1;
    }
}