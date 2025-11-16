using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

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

    private async Task OpenManagePasskeysModal(SearchUsersQueryResult.UserSummary user)
    {
        this.selectedUserForPasskeys = user;
        this.userPasskeys.Clear();
        this.selectedPasskeyIds.Clear();
        this.showPasskeysModal = true;

        await this.LoadUserPasskeys();
    }

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

    private void ClosePasskeysModal()
    {
        this.showPasskeysModal = false;
        this.selectedUserForPasskeys = null;
        this.userPasskeys.Clear();
        this.selectedPasskeyIds.Clear();
        this.StateHasChanged();
    }

    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }

        public UserStatus? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "CreatedAt";

        public bool SortDescending { get; set; } = true;
    }

    public abstract class BaseUserModel
    {
        public SystemRole Roles { get; set; } = 0;
    }

    private sealed class PasskeyInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Algorithm { get; set; } = string.Empty;

        public DateTime RegisteredAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime? RevokedAt { get; set; }
    }
}