using System.ComponentModel.DataAnnotations;
using BluQube.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Pages.Admin;

public partial class Users
{
    private bool showSuspendModal;
    private bool isSuspendingUser;
    private SearchUsersQueryResult.UserSummary? userToSuspend;

    private SuspendUserModel suspendUserModel = new();

    private void OpenSuspendUser(SearchUsersQueryResult.UserSummary user)
    {
        this.userToSuspend = user;
        this.suspendUserModel = new SuspendUserModel();
        this.showSuspendModal = true;
    }

    private void CloseSuspendModal()
    {
        this.showSuspendModal = false;
        this.userToSuspend = null;
        this.suspendUserModel = new SuspendUserModel();
    }

    private async Task HandleSuspendUser()
    {
        if (this.userToSuspend == null)
        {
            return;
        }

        this.isSuspendingUser = true;
        this.StateHasChanged();

        var result = await commander.Send(new SuspendAccountCommand(
            this.userToSuspend.Id, this.suspendUserModel.Reason, this.suspendUserModel.Notes));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"User {this.userToSuspend.Email} has been suspended.");
            this.CloseSuspendModal();
            await this.LoadUsers();
        }
        else
        {
            notificationService.ShowError("Failed to suspend user. Please try again.");
        }

        this.isSuspendingUser = false;
        this.StateHasChanged();
    }

    public class SuspendUserModel
    {
        [Range(1, 6, ErrorMessage = "Please select a reason for suspension.")]
        public AccountSuspensionReason Reason { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }
}