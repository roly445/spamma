using BluQube.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Code-behind for the Users page.
/// </summary>
public partial class Users
{
    private bool showUnsuspendModal;
    private bool isUnsuspendingUser;
    private SearchUsersQueryResult.UserSummary? userToUnsuspend;

    private void OpenUnsuspendUser(SearchUsersQueryResult.UserSummary user)
    {
        this.userToUnsuspend = user;
        this.showUnsuspendModal = true;
    }

    private void CloseUnsuspendModal()
    {
        this.showUnsuspendModal = false;
        this.userToUnsuspend = null;
    }

    private async Task HandleUnsuspendUser()
    {
        if (this.userToUnsuspend == null)
        {
            return;
        }

        this.isUnsuspendingUser = true;
        this.StateHasChanged();

        var result = await commander.Send(new UnsuspendAccountCommand(this.userToUnsuspend.Id));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"User {this.userToUnsuspend.Email} access has been restored.");
            this.CloseUnsuspendModal();
            await this.LoadUsers();
        }
        else
        {
            notificationService.ShowError("Failed to restore user access. Please try again.");
        }

        this.isUnsuspendingUser = false;
        this.StateHasChanged();
    }
}