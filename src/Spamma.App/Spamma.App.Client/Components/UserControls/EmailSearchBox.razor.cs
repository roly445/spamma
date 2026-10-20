using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Spamma.App.Client.Components.UserControls;

public partial class EmailSearchBox
{
    private static readonly SearchFilter[] Filters =
    [
        new("sender", "from:", "Sender name or address"),
        new("recipient", "to:", "Recipient name or address"),
        new("subject", "subject:", "Words in the subject"),
        new("body", "body:", "Words in the message"),
        new("attachment", "has:attachment", "Messages with attachments"),
        new("after", "after:", "Sent on or after a date"),
        new("before", "before:", "Sent before a date"),
    ];

    private ElementReference searchInput;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyPress { get; set; }

    [Parameter]
    public EventCallback OnClear { get; set; }

    [Parameter]
    public string? Error { get; set; }

    private Task HandleInput(ChangeEventArgs args) =>
        this.ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private async Task InsertFilter(string searchOperator)
    {
        var prefix = this.Value.TrimEnd();
        var query = prefix.Length == 0 ? searchOperator : $"{prefix} {searchOperator}";
        await this.ValueChanged.InvokeAsync(query);
        await this.searchInput.FocusAsync();
    }

    private sealed record SearchFilter(string Name, string Operator, string Description);
}
