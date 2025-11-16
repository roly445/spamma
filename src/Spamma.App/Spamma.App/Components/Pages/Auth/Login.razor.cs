using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.App.Components.Pages.Auth;

public partial class Login(ICommander commander) : ComponentBase
{
    private bool showSuccessMessage;

    [SupplyParameterFromForm(FormName = "LoginForm")]
    private LoginModel Model { get; set; } = new();

    private async Task HandleSendMagicLink()
    {
        var cmd = new StartAuthenticationCommand(this.Model.EmailAddress);
        await commander.Send(cmd);
        this.showSuccessMessage = true;
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string EmailAddress { get; set; } = string.Empty;
    }
}