using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Extensions;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

public partial class AppSettings(
    ICommandRunner commander,
    IQueryRunner querier,
    INotificationService notificationService,
    HttpClient httpClient,
    NavigationManager navigationManager) : ComponentBase
{
    private bool _catchAllEnabled;
    private bool _isSaving;
    private bool _showMaintenanceConfirm;
    private bool _isEnteringMaintenance;

    protected override async Task OnInitializedAsync()
    {
        await querier.ExecuteAsync(
            new GetEmailInboxSettingsQuery(),
            onSuccess: result =>
            {
                this._catchAllEnabled = result.CatchAllModeEnabled;
                return Task.CompletedTask;
            },
            onError: _ => Task.CompletedTask);
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
                onSuccess: () =>
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

                    return Task.CompletedTask;
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
}
