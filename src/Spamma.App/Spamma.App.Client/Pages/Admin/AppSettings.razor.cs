using BluQube.Commands;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Extensions;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.App.Client.Pages.Admin;

public partial class AppSettings(
    ICommandRunner commander,
    INotificationService notificationService,
    ISystemSettingsCache systemSettingsCache,
    HttpClient httpClient,
    NavigationManager navigationManager) : ComponentBase, IDisposable
{
    private bool _catchAllEnabled;
    private bool _isSaving;
    private bool _showMaintenanceConfirm;
    private bool _isEnteringMaintenance;
    private bool _disposed;

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
            systemSettingsCache.SettingsChanged -= this.OnSystemSettingsChanged;
        }

        this._disposed = true;
    }

    protected override async Task OnInitializedAsync()
    {
        await systemSettingsCache.InitializeAsync();
        this._catchAllEnabled = systemSettingsCache.Current.CatchAllModeEnabled;
        systemSettingsCache.SettingsChanged += this.OnSystemSettingsChanged;
    }

    private async Task ToggleCatchAll()
    {
        this._isSaving = true;
        this.StateHasChanged();

        try
        {
            var newValue = !this._catchAllEnabled;
            await commander.ExecuteAsync(
                new UpdateCatchAllModeCommand(newValue),
                onSuccess: async () =>
                {
                    this._catchAllEnabled = newValue;
                    await systemSettingsCache.ApplyAsync(systemSettingsCache.Current with { CatchAllModeEnabled = newValue });
                    if (newValue)
                    {
                        notificationService.ShowSuccess("Catch-All Mode enabled");
                    }
                    else
                    {
                        notificationService.ShowInfo("Catch-All Mode disabled");
                    }
                },
                onValidationErrors: _ => Task.CompletedTask,
                onError: _ =>
                {
                    notificationService.ShowError("Failed to update Catch-All Mode setting");
                    return Task.CompletedTask;
                });
        }
        finally
        {
            this._isSaving = false;
            this.StateHasChanged();
        }
    }

    private void ShowMaintenanceConfirm()
    {
        this._showMaintenanceConfirm = true;
    }

    private async Task ConfirmMaintenanceMode()
    {
        this._isEnteringMaintenance = true;
        this.StateHasChanged();

        try
        {
            var response = await httpClient.PostAsync("api/admin/maintenance", null);
            if (response.IsSuccessStatusCode)
            {
                navigationManager.NavigateTo("/", forceLoad: true);
            }
            else
            {
                notificationService.ShowError("Failed to enter maintenance mode");
                this._showMaintenanceConfirm = false;
            }
        }
        finally
        {
            this._isEnteringMaintenance = false;
            this.StateHasChanged();
        }
    }

    private string GetToggleClasses() =>
        this._catchAllEnabled ? "bg-blue-600" : "bg-gray-200";

    private string GetToggleKnobClasses() =>
        this._catchAllEnabled ? "translate-x-5" : "translate-x-0";

    private async Task OnSystemSettingsChanged(Spamma.Modules.Common.Client.Application.Queries.GetSystemSettingsQueryResult settings)
    {
        await this.InvokeAsync(() =>
        {
            this._catchAllEnabled = settings.CatchAllModeEnabled;
            this.StateHasChanged();
        });
    }
}
