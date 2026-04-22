using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

public partial class CatchAllSenders(
    ICommander commander,
    IQuerier querier,
    INotificationService notificationService) : ComponentBase
{
    private IReadOnlyList<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary> _items =
        Array.Empty<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary>();

    private bool _isLoading = true;
    private bool _showAddModal;
    private string _newSenderAddress = string.Empty;
    private string? _addError;
    private Guid? _selectedItemId;
    private GetCatchAllSenderAddressDetailQueryResult? _selectedDetail;
    private bool _isLoadingDetail;

    protected override async Task OnInitializedAsync()
    {
        await this.LoadAddresses();
    }

    private async Task LoadAddresses()
    {
        this._isLoading = true;
        this.StateHasChanged();

        var result = await querier.Send(new SearchCatchAllSenderAddressesQuery());
        this._items = result.Status == QueryResultStatus.Succeeded
            ? result.Data.Items
            : Array.Empty<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary>();

        this._isLoading = false;
        this.StateHasChanged();
    }

    private async Task HandleAddAddress()
    {
        this._addError = null;

        if (string.IsNullOrWhiteSpace(this._newSenderAddress))
        {
            this._addError = "Sender address is required.";
            return;
        }

        if (!this._newSenderAddress.Contains('@'))
        {
            this._addError = "Please enter a valid email address containing '@'.";
            return;
        }

        var result = await commander.Send(new AddCatchAllSenderAddressCommand(this._newSenderAddress.Trim()));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            this._showAddModal = false;
            this._newSenderAddress = string.Empty;
            notificationService.ShowSuccess("Sender address added successfully.");
            await this.LoadAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to add sender address. Please try again.");
        }
    }

    private async Task HandleRemove(Guid id)
    {
        var result = await commander.Send(new RemoveCatchAllSenderAddressCommand(id));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("Sender address removed.");
            await this.LoadAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to remove sender address. Please try again.");
        }
    }

    private async Task HandleSelectRow(Guid id)
    {
        if (this._selectedItemId == id)
        {
            this._selectedItemId = null;
            this._selectedDetail = null;
            return;
        }

        this._selectedItemId = id;
        this._selectedDetail = null;
        this._isLoadingDetail = true;
        this.StateHasChanged();

        var result = await querier.Send(new GetCatchAllSenderAddressDetailQuery(id));
        this._selectedDetail = result.Status == QueryResultStatus.Succeeded ? result.Data : null;

        this._isLoadingDetail = false;
        this.StateHasChanged();
    }

    private async Task HandleUnassignUser(Guid userId)
    {
        if (this._selectedItemId == null)
        {
            return;
        }

        var senderAddressId = this._selectedItemId.Value;
        var result = await commander.Send(new UnassignUserFromCatchAllSenderCommand(senderAddressId, userId));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("User unassigned.");
            this._isLoadingDetail = true;
            this.StateHasChanged();
            var detailResult = await querier.Send(new GetCatchAllSenderAddressDetailQuery(senderAddressId));
            this._selectedDetail = detailResult.Status == QueryResultStatus.Succeeded ? detailResult.Data : null;
            this._isLoadingDetail = false;
            this.StateHasChanged();
        }
        else
        {
            notificationService.ShowError("Failed to unassign user. Please try again.");
        }
    }

    private void OpenAddModal()
    {
        this._newSenderAddress = string.Empty;
        this._addError = null;
        this._showAddModal = true;
    }

    private void CloseAddModal()
    {
        this._showAddModal = false;
        this._newSenderAddress = string.Empty;
        this._addError = null;
    }

    private string GetRowClasses(SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary item)
    {
        if (item.IsRemoved)
        {
            return "opacity-50 cursor-default";
        }

        return this._selectedItemId == item.Id
            ? "bg-amber-50 border-l-2 border-amber-400 cursor-pointer hover:bg-amber-50"
            : "cursor-pointer hover:bg-amber-50";
    }
}
