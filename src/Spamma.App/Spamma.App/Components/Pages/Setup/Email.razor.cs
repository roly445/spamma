using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

public partial class Email(IAppConfigurationService appConfigurationService)
{
    private string? successMessage;
    private bool hasExistingEmailConfig;
    private IAppConfigurationService.EmailSettings? existingEmailConfig;

    [SupplyParameterFromForm(FormName = "EmailForm")]
    public EmailModel? Model { get; set; }

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        this.Layout.CurrentStep = "4";
        this.Layout.CompletedSteps.Add("0");
        this.Layout.CompletedSteps.Add("1");
        this.Layout.CompletedSteps.Add("2");
        this.Layout.CompletedSteps.Add("3");

        if (this.Model == null)
        {
            this.Model = new EmailModel();
            await this.CheckForExistingEmailConfig();

            if (!this.hasExistingEmailConfig)
            {
                // Set default values for new configuration
                this.Model.SmtpHost = "localhost";
                this.Model.SmtpPort = 1025;
                this.Model.FromEmail = "no-reply@spamma.dev";
                this.Model.FromName = "Spamma";
                this.Model.UseSsl = false;
            }
        }
    }

    private async Task CheckForExistingEmailConfig()
    {
        if (this.Model == null)
        {
            return;
        }

        try
        {
            this.existingEmailConfig = await appConfigurationService.GetEmailSettingsAsync();
            this.hasExistingEmailConfig = !string.IsNullOrEmpty(this.existingEmailConfig.SmtpHost) &&
                                          !string.IsNullOrEmpty(this.existingEmailConfig.FromEmail);

            if (this.hasExistingEmailConfig)
            {
                // Pre-populate the form with existing values for reconfigure scenario
                this.Model.SmtpHost = this.existingEmailConfig.SmtpHost;
                this.Model.SmtpPort = this.existingEmailConfig.SmtpPort;
                this.Model.Username = this.existingEmailConfig.Username;
                this.Model.Password = this.existingEmailConfig.Password;
                this.Model.FromEmail = this.existingEmailConfig.FromEmail;
                this.Model.FromName = this.existingEmailConfig.FromName;
                this.Model.UseSsl = this.existingEmailConfig.SmtpPort == 465; // Assume SSL for port 465
            }
        }
        catch
        {
            this.hasExistingEmailConfig = false;
        }
    }

    private async Task HandleSaveEmailSettings()
    {
        if (this.Model == null)
        {
            return;
        }

        var emailSettings = new IAppConfigurationService.EmailSettings
        {
            SmtpHost = this.Model.SmtpHost,
            SmtpPort = this.Model.SmtpPort,
            Username = this.Model.Username,
            Password = this.Model.Password,
            FromEmail = this.Model.FromEmail,
            FromName = this.Model.FromName,
            UseTls = this.Model.UseSsl,
        };

        await appConfigurationService.SaveEmailSettingsAsync(emailSettings);
        this.successMessage = "Email configuration saved successfully!";
        this.hasExistingEmailConfig = false; // Hide existing config UI
    }

    public class EmailModel
    {
        [Required(ErrorMessage = "SMTP Host is required")]
        public string SmtpHost { get; set; } = "localhost";

        [Required(ErrorMessage = "SMTP Port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int SmtpPort { get; set; } = 1025;

        public string? Username { get; set; }

        public string? Password { get; set; }

        [Required(ErrorMessage = "From Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string FromEmail { get; set; } = "no-reply@spamma.dev";

        [Required(ErrorMessage = "From Name is required")]
        public string FromName { get; set; } = "Spamma";

        public bool UseSsl { get; set; }
    }
}