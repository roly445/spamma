using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Code-behind for the hosting configuration setup step.
/// </summary>
public partial class Hosting(IAppConfigurationService appConfigurationService, ILogger<Hosting> logger)
{
    private string? successMessage;
    private bool hasExistingApplicationConfig;
    private IAppConfigurationService.ApplicationSettings? existingApplicationConfig;

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    [SupplyParameterFromForm(FormName = "HostingSettingsForm")]
    private HostingConfigurationModel? Model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        this.Layout.CurrentStep = "3";
        this.Layout.CompletedSteps.Add("0");
        this.Layout.CompletedSteps.Add("1");
        this.Layout.CompletedSteps.Add("2");

        if (this.Model == null)
        {
            var settings = await appConfigurationService.GetApplicationSettingsAsync();

            if (!string.IsNullOrWhiteSpace(settings.BaseUrl) || !string.IsNullOrWhiteSpace(settings.MailServerHostname))
            {
                this.hasExistingApplicationConfig = true;
                this.existingApplicationConfig = settings;
            }

            this.Model = new HostingConfigurationModel
            {
                BaseUrl = settings.BaseUrl ?? "https://",
                MailServerHostname = settings.MailServerHostname,
                MxPriority = settings.MxPriority,
            };
        }
    }

    private async Task HandleSaveApplicationSettings()
    {
        if (this.Model == null)
        {
            return;
        }

        logger.LogInformation(
            "Saving application configuration - BaseUrl: {BaseUrl}, MailServerHostname: {MailServerHostname}, Priority: {Priority}",
            this.Model.BaseUrl, this.Model.MailServerHostname, this.Model.MxPriority);

        var settings = new IAppConfigurationService.ApplicationSettings
        {
            BaseUrl = this.Model.BaseUrl?.Trim() ?? string.Empty,
            MailServerHostname = this.Model.MailServerHostname.Trim(),
            MxPriority = this.Model.MxPriority,
        };

        await appConfigurationService.SaveApplicationSettingsAsync(settings);

        logger.LogInformation("Application configuration saved successfully");

        this.successMessage = "Configuration saved successfully!";
    }

    public class HostingConfigurationModel
    {
        private string? _baseUrl;

        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? BaseUrl
        {
            get => this._baseUrl;
            set => this._baseUrl = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [Required(ErrorMessage = "Mail server hostname is required")]
        [RegularExpression(
            @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$",
            ErrorMessage = "Please enter a valid hostname")]
        public string MailServerHostname { get; set; } = string.Empty;

        [Required(ErrorMessage = "MX Priority is required")]
        [Range(1, 65535, ErrorMessage = "MX Priority must be between 1 and 65535")]
        public int MxPriority { get; set; } = 10;
    }
}