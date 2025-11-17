using System.ComponentModel.DataAnnotations;
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
    private EditUserModel _editUserModel = new();
    private bool showEditPanel;
    private SearchUsersQueryResult.UserSummary? selectedUser;
    private bool isSavingUser;

    private void OpenEditUser(SearchUsersQueryResult.UserSummary user)
    {
        this.selectedUser = user;
        this._editUserModel = new EditUserModel
        {
            DisplayName = user.DisplayName ?? string.Empty,
            Email = user.Email,
        };
        this.showEditPanel = true;
    }

    private void CloseEditPanel()
    {
        this.showEditPanel = false;
        this.selectedUser = null;
        this._editUserModel = new EditUserModel();
    }

    private async Task HandleSaveUser()
    {
        if (this.selectedUser == null)
        {
            return;
        }

        this.isSavingUser = true;
        this.StateHasChanged();

        var result = await commander.Send(new ChangeDetailsCommand(
            this.selectedUser.Id, this._editUserModel.Email, this._editUserModel.DisplayName, this._editUserModel.Roles));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"User '{this._editUserModel.DisplayName}' updated successfully.");
            this.CloseEditPanel();
            await this.LoadUsers();
        }
        else
        {
            notificationService.ShowError("Failed to update user. Please try again.");
        }

        this.isSavingUser = false;
        this.StateHasChanged();
    }

    private sealed class EditUserModel : BaseUserModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}