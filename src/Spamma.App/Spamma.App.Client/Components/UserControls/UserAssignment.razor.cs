using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Components.UserControls;

/// <summary>
/// Code-behind for the UserAssignment component.
/// </summary>
public partial class UserAssignment(ICommander commander, IQuerier querier, INotificationService notificationService) : ComponentBase
{
    private List<UserDetails> assignedUsers = new();
    private UserSearchRequest _searchRequest = new();
    private bool _isSearching;
    private bool showRemoveAssignmentModal;
    private bool isRemovingAssignment;
    private UserDetails? userToRemove;

    public enum Usage
    {
        DomainModeration,
        SubdomainModeration,
        SubdomainViewing,
    }

    [Parameter]
    public Guid EntityId { get; set; }

    [Parameter]
    public Usage EntityUsage { get; set; }

    [Parameter]
    public string Name { get; set; } = string.Empty;

    private string TypeName => this.EntityUsage == Usage.DomainModeration ? "domain" : "subdomain";

    public Task Reload()
    {
        return this.HandleUserSearch();
    }

    protected override Task OnInitializedAsync()
    {
        return this.HandleUserSearch();
    }

    private async Task HandleUserSearch()
    {
        this._isSearching = true;
        this.StateHasChanged();
        if (this.EntityUsage == Usage.DomainModeration)
        {
            var result = await querier.Send(new SearchDomainModeratorsQuery(
                SearchTerm: this._searchRequest.SearchTerm,
                SortBy: this._searchRequest.SortBy,
                SortDescending: this._searchRequest.SortDescending,
                DomainId: this.EntityId));
            if (result.Status != QueryResultStatus.Succeeded)
            {
                this.assignedUsers = new List<UserDetails>();
                return;
            }

            this.assignedUsers = result.Data.Items.Select(x => new UserDetails(x.UserId, x.DisplayName, x.Email)).ToList();
        }
        else if (this.EntityUsage == Usage.SubdomainModeration)
        {
            var result = await querier.Send(new SearchSubdomainModeratorsQuery(
                SearchTerm: this._searchRequest.SearchTerm,
                SortBy: this._searchRequest.SortBy,
                SortDescending: this._searchRequest.SortDescending,
                SubdomainId: this.EntityId));
            if (result.Status != QueryResultStatus.Succeeded)
            {
                this.assignedUsers = new List<UserDetails>();
                return;
            }

            this.assignedUsers = result.Data.Items.Select(x => new UserDetails(x.UserId, x.DisplayName, x.Email)).ToList();
        }
        else if (this.EntityUsage == Usage.SubdomainViewing)
        {
            var result = await querier.Send(new SearchSubdomainViewersQuery(
                SearchTerm: this._searchRequest.SearchTerm,
                SortBy: this._searchRequest.SortBy,
                SortDescending: this._searchRequest.SortDescending,
                SubdomainId: this.EntityId));
            if (result.Status != QueryResultStatus.Succeeded)
            {
                this.assignedUsers = new List<UserDetails>();
                return;
            }

            this.assignedUsers = result.Data.Items.Select(x => new UserDetails(x.UserId, x.DisplayName, x.Email)).ToList();
        }

        this._isSearching = false;
        this.StateHasChanged();
    }

    private void CloseRemoveAssignmentModal()
    {
        this.showRemoveAssignmentModal = false;
        this.userToRemove = null;
    }

    private async Task HandleRemoveAssignment()
    {
        if (this.EntityId == Guid.Empty || this.userToRemove is null)
        {
            return;
        }

        this.isRemovingAssignment = true;
        this.StateHasChanged();

        CommandResult result = this.EntityUsage switch
        {
            Usage.DomainModeration => await commander.Send(
                new RemoveModeratorFromDomainCommand(this.EntityId, this.userToRemove.Id)),
            Usage.SubdomainModeration => await commander.Send(
                new RemoveModeratorFromSubdomainCommand(this.EntityId, this.userToRemove.Id)),
            _ => await commander.Send(new RemoveViewerFromSubdomainCommand(this.EntityId, this.userToRemove.Id)),
        };
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"User '{this.userToRemove.FullName}' removed from {this.TypeName} '{this.Name}' successfully!");
            this.CloseRemoveAssignmentModal();
            await this.Reload();
        }
        else
        {
            notificationService.ShowError("Failed to remove user assignment. Please try again.");
        }

        this.isRemovingAssignment = false;
        this.StateHasChanged();
    }

    private void OpenRemoveAssignmentModal(UserDetails user)
    {
        this.userToRemove = user;
        this.showRemoveAssignmentModal = true;
    }

    private sealed class UserSearchRequest
    {
        public string SearchTerm { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string SortBy { get; set; } = "Name";

        public bool SortDescending { get; set; } = false;
    }

    public record UserDetails(Guid Id, string FullName, string Email);
}