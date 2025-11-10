using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Spamma.App.Components.Pages;

/// <summary>
/// Backing code for the Error Razor component.
/// </summary>
public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}