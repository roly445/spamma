using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Code-behind for the Setup Complete page.
/// </summary>
public partial class Complete(
    IInMemorySetupAuthService setupAuthService,
    IAppConfigurationService appConfigurationService,
    IConfiguration configuration,
    ILogger<Complete> logger,
    NavigationManager navigationManager)
{
    private readonly SetupStatusInfo setupStatus = new();
    private readonly List<MissingStep> missingSteps = new();
    private bool isSetupComplete;

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    [SupplyParameterFromForm(FormName = "SetupCompletedForm")]
    public CompleteModel? CompModel { get; set; }

    protected override async Task OnInitializedAsync()
    {
        this.CompModel ??= new();
        this.Layout.CompletedSteps.Add("0");
        this.Layout.CompletedSteps.Add("1");
        this.Layout.CompletedSteps.Add("2");
        this.Layout.CompletedSteps.Add("3");
        this.Layout.CompletedSteps.Add("4");
        this.Layout.CompletedSteps.Add("5");
        this.Layout.CompletedSteps.Add("6");
        this.Layout.CurrentStep = "7";
        await this.ValidateSetupCompletion();
    }

    private async Task ValidateSetupCompletion()
    {
        try
        {
            logger.LogInformation("Validating setup completion status");

            // Check security keys
            var keySettings = await appConfigurationService.GetKeySettingsAsync();
            this.setupStatus.HasSecurityKeys = !string.IsNullOrEmpty(keySettings.SigningKey);

            // Check email configuration
            var emailSettings = await appConfigurationService.GetEmailSettingsAsync();
            this.setupStatus.HasEmailConfiguration = !string.IsNullOrEmpty(emailSettings.SmtpHost) && !string.IsNullOrEmpty(emailSettings.FromEmail);

            // Check admin user
            var primaryUserSettings = await appConfigurationService.GetPrimaryUserSettingsAsync();
            this.setupStatus.HasAdminUser = primaryUserSettings.PrimaryUserId != Guid.Empty;

            // TLS certificates are optional - always considered configured for completion
            this.setupStatus.HasTlsCertificate = true;

            // Determine overall completion status - certificates are NOT required
            this.isSetupComplete = this.setupStatus.HasSecurityKeys &&
                             this.setupStatus.HasEmailConfiguration &&
                             this.setupStatus.HasAdminUser;

            // Build list of missing steps
            this.BuildMissingStepsList();

            logger.LogInformation("Setup validation complete. IsComplete: {IsComplete}", this.isSetupComplete);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating setup completion");
            this.isSetupComplete = false;
        }
    }

    private void BuildMissingStepsList()
    {
        this.missingSteps.Clear();

        if (!this.setupStatus.HasSecurityKeys)
        {
            this.missingSteps.Add(new MissingStep
            {
                Title = "Security Keys Missing",
                Description = "Generate signing and JWT keys for secure authentication.",
                ActionUrl = "/setup/keys",
            });
        }

        if (!this.setupStatus.HasEmailConfiguration)
        {
            this.missingSteps.Add(new MissingStep
            {
                Title = "Email Configuration Missing",
                Description = "Configure SMTP settings for sending authentication emails.",
                ActionUrl = "/setup/email",
            });
        }

        if (!this.setupStatus.HasAdminUser)
        {
            this.missingSteps.Add(new MissingStep
            {
                Title = "Admin User Not Created",
                Description = "Create the primary administrator account.",
                ActionUrl = "/setup/admin",
            });
        }
    }

    private async Task ReloadConfiguration()
    {
        try
        {
            logger.LogInformation("Reloading application configuration");

            // Reload configuration root if available
            if (configuration is IConfigurationRoot configRoot)
            {
                configRoot.Reload();
                await appConfigurationService.MarkSetupCompleteAsync();
                setupAuthService.DisableSetupMode();
                logger.LogInformation("Configuration root reloaded successfully");
            }

            logger.LogInformation("Configuration reload completed successfully");
            navigationManager.NavigateTo("/login", true);
        }
        catch (Exception ex) when (ex is not NavigationException)
        {
            logger.LogError(ex, "Error reloading configuration");
        }
    }

    public class CompleteModel
    {
        public string SomeValue { get; set; } = string.Empty;
    }

    private sealed class SetupStatusInfo
    {
        public bool HasSecurityKeys { get; set; }

        public bool HasEmailConfiguration { get; set; }

        public bool HasAdminUser { get; set; }

        public bool HasTlsCertificate { get; set; }
    }

    private sealed class MissingStep
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? ActionUrl { get; set; }
    }
}