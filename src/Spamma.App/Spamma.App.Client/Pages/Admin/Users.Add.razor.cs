using System.ComponentModel.DataAnnotations;
using BluQube.Constants;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the users admin page - add user functionality.
/// </summary>
public partial class Users
{
    private bool showAddUserModal;
    private bool isAddingUser;

    private AddUserModel _addUserModel = new();

    private void OpenAddUserModal()
    {
        this._addUserModel = new AddUserModel();
        this.showAddUserModal = true;
    }

    private void CloseAddUserModal()
    {
        this.showAddUserModal = false;
        this._addUserModel = new AddUserModel();
    }

    private async Task HandleAddUser()
    {
        this.isAddingUser = true;
        this.StateHasChanged();

        var result = await commander.Send(new CreateUserCommand(
            Guid.NewGuid(),
            this._addUserModel.DisplayName,
            this._addUserModel.Email,
            this._addUserModel.SendWelcome,
            this._addUserModel.Roles));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"User {this._addUserModel.Email} has been created successfully.");
            this.CloseAddUserModal();
            await this.LoadUsers();
        }
        else
        {
            notificationService.ShowWarning("Failed to create user. Please try again.");
        }

        this.isAddingUser = false;
        this.StateHasChanged();
    }

    private sealed class AddUserModel : BaseUserModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter their display name.")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters.")]
        public string DisplayName { get; set; } = string.Empty;

        public bool SendWelcome { get; set; } = true;
    }
}