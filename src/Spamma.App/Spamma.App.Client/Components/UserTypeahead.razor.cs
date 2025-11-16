using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Components;

public partial class UserTypeahead(IQuerier querier) : IDisposable
{
    private string? searchTerm;
    private bool isDropdownVisible;
    private bool isLoading;
    private List<SearchUsersQueryResult.UserSummary> suggestions = new();
    private Timer? _debounceTimer;
    private bool _disposed;

    [Parameter]
    public string? Placeholder { get; set; } = "Search users...";

    [Parameter]
    public EventCallback<SearchUsersQueryResult.UserSummary> OnUserSelected { get; set; }

    [Parameter]
    public Guid? Value { get; set; }

    [Parameter]
    public EventCallback<Guid> ValueChanged { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string? SearchTerm
    {
        get => this.searchTerm;
        set
        {
            if (this.searchTerm == value)
            {
                return;
            }

            this.searchTerm = value;
            this._debounceTimer?.Dispose();
            this._debounceTimer = new Timer(
                async void (_) =>
            {
                await this.InvokeAsync(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(this.searchTerm))
                    {
                        await this.SearchUsers();
                    }
                    else
                    {
                        this.suggestions.Clear();
                    }

                    this.StateHasChanged();
                });
            }, null, 300, Timeout.Infinite);
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            this._debounceTimer?.Dispose();
        }

        this._disposed = true;
    }

    private async Task SearchUsers()
    {
        try
        {
            this.isLoading = true;
            this.StateHasChanged();

            var query = new SearchUsersQuery(
                SearchTerm: this.SearchTerm,
                Status: UserStatus.Active,
                PageSize: 5);

            var result = await querier.Send(query);
            this.suggestions = result.Data.Items.ToList();
        }
        finally
        {
            this.isLoading = false;
            this.StateHasChanged();
        }
    }

    private async Task SelectUser(SearchUsersQueryResult.UserSummary user)
    {
        this.SearchTerm = user.Email;
        this.isDropdownVisible = false;
        await this.ValueChanged.InvokeAsync(user.Id);
        await this.OnUserSelected.InvokeAsync(user);
    }

    private void HandleFocus()
    {
        this.isDropdownVisible = true;
        if (!string.IsNullOrWhiteSpace(this.SearchTerm))
        {
            this._debounceTimer?.Dispose();
            this._debounceTimer = new Timer(
                async void (_) =>
            {
                await this.InvokeAsync(this.SearchUsers);
            }, null, 0, Timeout.Infinite);
        }
    }

    private async Task HandleBlur()
    {
        // Small delay to allow clicking on suggestions
        await Task.Delay(500);
        this.isDropdownVisible = false;
        this.StateHasChanged();
    }
}