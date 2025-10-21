using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
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
}