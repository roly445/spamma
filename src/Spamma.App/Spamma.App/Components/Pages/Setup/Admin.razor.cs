using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Code-behind for the admin setup page.
/// </summary>
public partial class Admin(
    IAppConfigurationService appConfigurationService,
    IInternalQueryStore internalQueryStore,
    ILogger<Admin> logger, ICommander commander)
{
    private string? successMessage;

    private bool hasExistingAdminUser;

    [SupplyParameterFromForm(FormName = "AdminForm")]
    public AdminModel? Model { get; set; }

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        this.Layout.CurrentStep = "6";
        this.Layout.CompletedSteps.Add("0");
        this.Layout.CompletedSteps.Add("1");
        this.Layout.CompletedSteps.Add("2");
        this.Layout.CompletedSteps.Add("3");
        this.Layout.CompletedSteps.Add("4");
        this.Layout.CompletedSteps.Add("5");

        if (this.Model == null)
        {
            this.Model = new AdminModel();
            var existingKeys = await appConfigurationService.GetPrimaryUserSettingsAsync();
            this.hasExistingAdminUser = existingKeys.PrimaryUserId != Guid.Empty;
        }
    }

    private async Task HandleCreateAdminUser()
    {
        if (this.Model == null)
        {
            return;
        }

        var userId = Guid.NewGuid();
        logger.LogInformation("Creating admin user with email: {Email}", this.Model.AdminEmail);
        var cmd = new CreateUserCommand(userId, this.Model.AdminName, this.Model.AdminEmail, false, 0);
        internalQueryStore.StoreQueryRef(cmd);
        await commander.Send(cmd);

        this.successMessage = "Admin user created successfully!";
        logger.LogInformation("Setup completed successfully for admin user: {Email}", this.Model.AdminEmail);

        await appConfigurationService.SavePrimaryUserAsync(new IAppConfigurationService.PrimaryUserSettings
        {
            PrimaryUserId = userId,
        });
    }

    public class AdminModel
    {
        [Required(ErrorMessage = "Admin email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string AdminEmail { get; set; } = string.Empty;

        public string AdminName { get; set; } = string.Empty;
    }
}