using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Spamma.App.Components.Pages;

/// <summary>
/// Code-behind for the error component.
/// </summary>
public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

    protected override void OnInitialized() =>
        this.RequestId = Activity.Current?.Id ?? this.HttpContext?.TraceIdentifier;
}