using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Components.UserControls.Subdomain;

/// <summary>
/// Code-behind for the CheckMxRecord component.
/// </summary>
public partial class CheckMxRecord(ICommander commander) : ComponentBase
{
    private bool isVisible;
    private DataModel? _dataModel;
    private bool isProcessing;

    [Parameter]
    public EventCallback<CheckMxRecordCommandResult> OnChecked { get; set; }

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

    private async Task CheckMxRecords()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var result = await commander.Send(new CheckMxRecordCommand(this._dataModel.Id));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            await this.OnChecked.InvokeAsync(result.Data);
            if (result.Data.MxStatus == MxStatus.Valid)
            {
                this.isVisible = false;
            }
        }
    }

    public record DataModel(Guid Id, string FullName);
}