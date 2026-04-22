using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.App.Client.Pages.Admin;

public partial class AppSettings(
    ICommandRunner commander,
    IOptions<Spamma.App.Client.Infrastructure.Contracts.Settings> settings,
    INotificationService notificationService,
    HttpClient httpClient,
    NavigationManager navigationManager) : ComponentBase
{
    private bool _catchAllEnabled;
    private bool _isSaving;
    private bool _showMaintenanceConfirm;
    private bool _isEnteringMaintenance;

    protected override void OnInitialized()
    {
        this._catchAllEnabled = settings.Value.CatchAllModeEnabled;
    }

    private async Task ToggleCatchAll()
    {
        this._isSaving = true;
        this.StateHasChanged();

        try
        {
            var newValue = !this._catchAllEnabled;
            var result = await commander.Send(new UpdateCatchAllModeCommand(newValue));

            if (result.Status == CommandResultStatus.Succeeded)
            {
                this._catchAllEnabled = newValue;

                if (newValue)
                {
                    notificationService.ShowSuccess("Catch-All Mode enabled");
                }
                else
                {
                    notificationService.ShowInfo("Catch-All Mode disabled");
                }
            }
            else
            {
                notificationService.ShowError("Failed to update Catch-All Mode setting");
            }
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
        this._catchAllEnabled ? "bg-amber-500" : "bg-gray-200";

    private string GetToggleKnobClasses() =>
        this._catchAllEnabled ? "translate-x-5" : "translate-x-0";
}
