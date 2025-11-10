using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the users admin page.
/// </summary>
public partial class Users(ICommander commander, IQuerier querier, INotificationService notificationService) : ComponentBase
{
    private UserSearchRequest _searchRequest = new();
    private SearchUsersQueryResult pagedResult = new(new List<SearchUsersQueryResult.UserSummary>(), 0, 0, 0, 0);
    private GetUserStatsQueryResult? userStats;
    private bool isLoading = true;
    private bool showPasskeysModal;
    private SearchUsersQueryResult.UserSummary? selectedUserForPasskeys;
    private List<PasskeyInfo> userPasskeys = new();
    private HashSet<Guid> selectedPasskeyIds = new();
    private bool isLoadingPasskeys;
    private bool isRevokingPasskeys;

    protected override async Task OnInitializedAsync()
    {
        await this.LoadUsers();
    }

    private static string GetStatusClasses(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800",
            UserStatus.Inactive => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
            UserStatus.Suspended => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800",
            _ => "inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800",
        };
    }

    private static string GetStatusText(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "Active",
            UserStatus.Inactive => "Inactive",
            UserStatus.Suspended => "Suspended",
            _ => "Unknown",
        };
    }

    private static bool IsRoleSelected(SystemRole role, SystemRole roles)
    {
        return (roles & role) == role;
    }

    private static void ToggleRole(SystemRole role, BaseUserModel existingRole, object checkedValue)
    {
        bool isChecked = checkedValue is bool b && b;
        if (isChecked)
        {
            existingRole.Roles |= role;
        }
        else
        {
            existingRole.Roles &= ~role;
        }
    }

    private async Task HandleSearch()
    {
        this._searchRequest.Page = 1;
        await this.LoadUsers();
    }

    private async Task GoToPage(int page)
    {
        this._searchRequest.Page = page;
        await this.LoadUsers();
    }

    private async Task LoadUsers()
    {
        this.isLoading = true;
        this.StateHasChanged();

        var res = await querier.Send(new SearchUsersQuery(
            SearchTerm: this._searchRequest.SearchTerm,
            Status: this._searchRequest.Status,
            SortBy: this._searchRequest.SortBy,
            SortDescending: this._searchRequest.SortDescending,
            Page: this._searchRequest.Page,
            PageSize: this._searchRequest.PageSize));
        if (res.Status == QueryResultStatus.Succeeded)
        {
            this.pagedResult = res.Data;
        }
        else
        {
            notificationService.ShowError("Failed to load users. Please try again.");
            this.pagedResult = new SearchUsersQueryResult(
                new List<SearchUsersQueryResult.UserSummary>(), 0, this._searchRequest.Page, this._searchRequest.PageSize, 0);
        }

        var statsResult = await querier.Send(new GetUserStatsQuery());
        if (statsResult.Status == QueryResultStatus.Succeeded)
        {
            this.userStats = statsResult.Data;
        }

        this.isLoading = false;
        this.StateHasChanged();
    }

    /// <summary>
    /// Opens the passkeys management modal for a specific user.
    /// </summary>
    private async Task OpenManagePasskeysModal(SearchUsersQueryResult.UserSummary user)
    {
        this.selectedUserForPasskeys = user;
        this.userPasskeys.Clear();
        this.selectedPasskeyIds.Clear();
        this.showPasskeysModal = true;

        await this.LoadUserPasskeys();
    }

    /// <summary>
    /// Loads passkeys for the selected user.
    /// </summary>
    private async Task LoadUserPasskeys()
    {
        this.isLoadingPasskeys = true;
        this.StateHasChanged();

        var result = await querier.Send(new GetUserPasskeysQuery(UserId: this.selectedUserForPasskeys!.Id));

        if (result.Status == QueryResultStatus.Succeeded && result.Data != null)
        {
            this.userPasskeys = result.Data.Passkeys
                .Select(p => new PasskeyInfo
                {
                    Id = p.Id,
                    DisplayName = p.DisplayName,
                    Algorithm = p.Algorithm,
                    RegisteredAt = p.RegisteredAt,
                    LastUsedAt = p.LastUsedAt,
                    IsRevoked = p.IsRevoked,
                    RevokedAt = p.RevokedAt,
                })
                .ToList();
        }
        else
        {
            notificationService.ShowError("Failed to load passkeys. Please try again.");
            this.userPasskeys.Clear();
        }

        this.isLoadingPasskeys = false;
        this.StateHasChanged();
    }

    /// <summary>
    /// Toggles selection of a passkey.
    /// </summary>
    private void TogglePasskeySelection(Guid passkeyId)
    {
        if (this.selectedPasskeyIds.Contains(passkeyId))
        {
            this.selectedPasskeyIds.Remove(passkeyId);
        }
        else
        {
            this.selectedPasskeyIds.Add(passkeyId);
        }
    }

    /// <summary>
    /// Toggles selection of all non-revoked passkeys.
    /// </summary>
    private void ToggleSelectAll()
    {
        var activePasskeys = this.userPasskeys.Where(p => !p.IsRevoked).Select(p => p.Id).ToList();

        if (this.selectedPasskeyIds.Count == activePasskeys.Count && activePasskeys.All(id => this.selectedPasskeyIds.Contains(id)))
        {
            this.selectedPasskeyIds.Clear();
        }
        else
        {
            this.selectedPasskeyIds.Clear();
            foreach (var id in activePasskeys)
            {
                this.selectedPasskeyIds.Add(id);
            }
        }
    }

    /// <summary>
    /// Revokes the selected passkeys.
    /// </summary>
    private async Task HandleRevokeSelectedPasskeys()
    {
        if (this.selectedPasskeyIds.Count == 0 || this.selectedUserForPasskeys == null)
        {
            return;
        }

        this.isRevokingPasskeys = true;
        this.StateHasChanged();

        int successCount = 0;
        int failureCount = 0;

        foreach (var passkeyId in this.selectedPasskeyIds)
        {
            var result = await commander.Send(new RevokeUserPasskeyCommand(
                UserId: this.selectedUserForPasskeys.Id,
                PasskeyId: passkeyId));

            if (result.Status == CommandResultStatus.Succeeded)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        this.isRevokingPasskeys = false;

        if (failureCount == 0)
        {
            notificationService.ShowSuccess($"Successfully revoked {successCount} passkey{(successCount != 1 ? "s" : string.Empty)}.");
            await this.LoadUserPasskeys();
            this.selectedPasskeyIds.Clear();
        }
        else if (successCount == 0)
        {
            notificationService.ShowError("Failed to revoke passkeys. Please try again.");
        }
        else
        {
            notificationService.ShowWarning($"Revoked {successCount} passkey{(successCount != 1 ? "s" : string.Empty)} but {failureCount} failed.");
            await this.LoadUserPasskeys();
            this.selectedPasskeyIds.Clear();
        }

        this.StateHasChanged();
    }

    /// <summary>
    /// Closes the passkeys management modal.
    /// </summary>
    private void ClosePasskeysModal()
    {
        this.showPasskeysModal = false;
        this.selectedUserForPasskeys = null;
        this.userPasskeys.Clear();
        this.selectedPasskeyIds.Clear();
        this.StateHasChanged();
    }

    /// <summary>
    /// Request parameters for searching users.
    /// </summary>
    public class UserSearchRequest
    {
        /// <summary>
        /// Gets or sets the search term to filter users.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the user status filter.
        /// </summary>
        public UserStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the field to sort by.
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Gets or sets a value indicating whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Base class for user models with role support.
    /// </summary>
    public abstract class BaseUserModel
    {
        /// <summary>
        /// Gets or sets the user roles.
        /// </summary>
        public SystemRole Roles { get; set; } = 0;
    }

    /// <summary>
    /// Information about a single passkey.
    /// </summary>
    private sealed class PasskeyInfo
    {
        /// <summary>
        /// Gets or sets the passkey ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the algorithm.
        /// </summary>
        public string Algorithm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the registration date/time.
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// Gets or sets the last used date/time.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the passkey is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation date/time.
        /// </summary>
        public DateTime? RevokedAt { get; set; }
    }
}