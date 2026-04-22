using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.App.Components.Pages.Auth;

public partial class Login(ICommander commander, ILogger<Login> logger) : ComponentBase
{
    private bool showSuccessMessage;

    [SupplyParameterFromForm(FormName = "LoginForm")]
    private LoginModel? Model { get; set; }

    protected override void OnInitialized() => this.Model ??= new();

    private async Task HandleSendMagicLink()
    {
        logger.LogInformation("CANARY — HandleSendMagicLink invoked for {EmailAddress}", this.Model!.EmailAddress);

        var cmd = new StartAuthenticationCommand(this.Model!.EmailAddress);
        var result = await commander.Send(cmd);

        if (result.Status != CommandResultStatus.Succeeded)
        {
            logger.LogWarning(
                "StartAuthenticationCommand failed for {EmailAddress}: {ErrorMessage}",
                this.Model.EmailAddress,
                result.ErrorData?.Message);
        }

        this.showSuccessMessage = true;
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string EmailAddress { get; set; } = string.Empty;
    }
}