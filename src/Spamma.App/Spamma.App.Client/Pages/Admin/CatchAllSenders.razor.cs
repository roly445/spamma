using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

public partial class CatchAllSenders(
    ICommandRunner commander,
    IQueryRunner querier,
    INotificationService notificationService) : ComponentBase
{
    private IReadOnlyList<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary> _items =
        Array.Empty<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary>();

    private bool _isLoading = true;
    private bool _showAddModal;
    private string _newSenderAddress = string.Empty;
    private string? _addError;
    private string _searchTerm = string.Empty;

    private bool _showManageModal;
    private Guid? _managingItemId;
    private GetCatchAllSenderAddressDetailQueryResult? _managingDetail;
    private bool _isLoadingManageDetail;
    private Dictionary<Guid, string> _assignedUserEmails = new();

    private string _assignSearchTerm = string.Empty;
    private IReadOnlyList<SearchUsersQueryResult.UserSummary> _assignSearchResults =
        Array.Empty<SearchUsersQueryResult.UserSummary>();

    private bool _isSearchingUsers;

    private IEnumerable<SearchCatchAllSenderAddressesQueryResult.SenderAddressSummary> FilteredItems =>
        string.IsNullOrEmpty(this._searchTerm)
            ? this._items
            : this._items.Where(a => a.SenderAddress.Contains(this._searchTerm, StringComparison.OrdinalIgnoreCase));

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

    private async Task OpenManageModal(Guid id)
    {
        this._managingItemId = id;
        this._showManageModal = true;
        this._managingDetail = null;
        this._assignedUserEmails = new Dictionary<Guid, string>();
        this._isLoadingManageDetail = true;
        this._assignSearchTerm = string.Empty;
        this._assignSearchResults = Array.Empty<SearchUsersQueryResult.UserSummary>();
        this.StateHasChanged();

        var result = await querier.Send(new GetCatchAllSenderAddressDetailQuery(id));
        if (result.Status == QueryResultStatus.Succeeded)
        {
            this._managingDetail = result.Data;
            await this.LoadAssignedUserEmails(result.Data.AssignedUserIds);
        }

        this._isLoadingManageDetail = false;
        this.StateHasChanged();
    }

    private async Task LoadAssignedUserEmails(IReadOnlyList<Guid> userIds)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var result = await querier.Send(new SearchUsersQuery(PageSize: 100));
        if (result.Status != QueryResultStatus.Succeeded)
        {
            return;
        }

        this._assignedUserEmails = result.Data.Items
            .Where(u => userIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.DisplayName != null ? $"{u.DisplayName} ({u.Email})" : u.Email);
    }

    private void CloseManageModal()
    {
        this._showManageModal = false;
        this._managingItemId = null;
        this._managingDetail = null;
        this._assignSearchTerm = string.Empty;
        this._assignSearchResults = Array.Empty<SearchUsersQueryResult.UserSummary>();
    }

    private async Task HandleUserSearch()
    {
        if (string.IsNullOrWhiteSpace(this._assignSearchTerm))
        {
            return;
        }

        this._isSearchingUsers = true;
        this.StateHasChanged();

        var result = await querier.Send(new SearchUsersQuery(SearchTerm: this._assignSearchTerm, PageSize: 5));
        this._assignSearchResults = result.Status == QueryResultStatus.Succeeded
            ? result.Data.Items
            : Array.Empty<SearchUsersQueryResult.UserSummary>();

        this._isSearchingUsers = false;
        this.StateHasChanged();
    }

    private async Task HandleAssignUser(Guid userId)
    {
        if (this._managingItemId == null)
        {
            return;
        }

        var result = await commander.Send(new AssignUserToCatchAllSenderCommand(this._managingItemId.Value, userId));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("User assigned.");
            this._assignSearchResults = Array.Empty<SearchUsersQueryResult.UserSummary>();
            this._assignSearchTerm = string.Empty;
            await this.RefreshManageDetail();
            await this.LoadAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to assign user. Please try again.");
        }
    }

    private async Task HandleUnassignUser(Guid userId)
    {
        if (this._managingItemId == null)
        {
            return;
        }

        var result = await commander.Send(new UnassignUserFromCatchAllSenderCommand(this._managingItemId.Value, userId));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("User unassigned.");
            await this.RefreshManageDetail();
            await this.LoadAddresses();
        }
        else
        {
            notificationService.ShowError("Failed to unassign user. Please try again.");
        }
    }

    private async Task RefreshManageDetail()
    {
        if (this._managingItemId == null)
        {
            return;
        }

        this._isLoadingManageDetail = true;
        this.StateHasChanged();

        var detailResult = await querier.Send(new GetCatchAllSenderAddressDetailQuery(this._managingItemId.Value));
        if (detailResult.Status == QueryResultStatus.Succeeded)
        {
            this._managingDetail = detailResult.Data;
            await this.LoadAssignedUserEmails(detailResult.Data.AssignedUserIds);
        }

        this._isLoadingManageDetail = false;
        this.StateHasChanged();
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
}
