using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.App.Client.Components.UserControls.Subdomain;

public partial class UnsuspendSubdomain(ICommander commander, INotificationService notificationService) : ComponentBase
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

    private void Close()
    {
        this.isVisible = false;
        this._dataModel = null;
    }

    private async Task HandleUnsuspendSubdomain()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var result = await commander.Send(new UnsuspendSubdomainCommand(this._dataModel.Id));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"Subdomain '{this._dataModel.FullName}' unsuspended successfully.");
            await this.OnUnsuspended.InvokeAsync();
            this.Close();
        }
        else
        {
            notificationService.ShowError($"Subdomain '{this._dataModel.FullName}' failed to unsuspend.");
        }

        this.isProcessing = false;
        this.StateHasChanged();
    }

    public record DataModel(Guid Id, string FullName, DateTime CreatedAt);
}