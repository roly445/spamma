using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.App.Client.Components.UserControls.Domain;

/// <summary>
/// Backing code for the unsuspend domain modal.
/// </summary>
public partial class UnsuspendDomain(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private bool isVisible;
    private bool isProcessing;
    private DataModel? _dataModel;

    [Parameter]
    public EventCallback OnUnsuspended { get; set; }

    public void Open(DataModel dataModel)
    {
        this._dataModel = dataModel;
        this.isVisible = true;
    }

    public void Close()
    {
        this.isVisible = false;
        this._dataModel = null;
    }

    private async Task HandleUnsuspendDomain()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var result = await commander.Send(new UnsuspendDomainCommand(this._dataModel.DomainId));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"Domain '{this._dataModel.DomainName}' unsuspended successfully.");
            await this.OnUnsuspended.InvokeAsync();
            this.Close();
        }
        else
        {
            notificationService.ShowError("Failed to unsuspend domain. Please try again.");
        }

        this.isProcessing = false;
        this.StateHasChanged();
    }

    public record DataModel(Guid DomainId, string DomainName);
}