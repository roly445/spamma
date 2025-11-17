using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Client.Components.UserControls;

/// <summary>
/// Code-behind for the AddUser component.
/// </summary>
public partial class AddUser(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private bool showAssignUserModal;
    private bool isAssigningUser;
    private AssignUserModel assignUserModel = new();

    public enum Usage
    {
        Domain,
        Subdomain,
        Viewer,
    }

    [Parameter]
    public EventCallback OnAdded { get; set; }

    [Parameter]
    public Usage EntityUsage { get; set; }

    [Parameter]
    public Guid EntityId { get; set; }

    [Parameter]
    public string EntityName { get; set; } = string.Empty;

    private string UserTypeName => this.EntityUsage is Usage.Domain or Usage.Subdomain ? "moderator" : "viewer";

    public void Open()
    {
         this.assignUserModel = new AssignUserModel();
         this.showAssignUserModal = true;
    }

    private void Close()
    {
        this.showAssignUserModal = false;
        this.assignUserModel = new AssignUserModel();
    }

    private async Task HandleAssignUser()
    {
        this.isAssigningUser = true;
        this.StateHasChanged();

        CommandResult result = this.EntityUsage switch
        {
            Usage.Domain => await commander.Send(new AddModeratorToDomainCommand(
                this.EntityId,
                this.assignUserModel.UserId)),
            Usage.Subdomain => await commander.Send(
                new AddModeratorToSubdomainCommand(this.EntityId, this.assignUserModel.UserId)),
            _ => await commander.Send(new AddViewerToSubdomainCommand(this.EntityId, this.assignUserModel.UserId)),
        };

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"User assigned as {this.UserTypeName} to '{this.EntityName}' successfully!");
            await this.OnAdded.InvokeAsync();
            this.Close();
        }
        else
        {
            notificationService.ShowError($"Failed to assign as {this.UserTypeName} to '{this.EntityName}'!");
        }

        this.isAssigningUser = false;
        this.StateHasChanged();
    }

    private void HandleUserSelected(SearchUsersQueryResult.UserSummary user)
    {
        this.assignUserModel.UserId = user.Id;
    }

    private sealed class AssignUserModel
    {
        [Required]
        public Guid UserId { get; set; } = Guid.Empty;
    }
}