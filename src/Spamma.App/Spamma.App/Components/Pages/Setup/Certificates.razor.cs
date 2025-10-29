using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Setup page for SSL/TLS certificate configuration.
/// </summary>
public partial class Certificates(IAppConfigurationService appConfigurationService)
{
    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    [SupplyParameterFromForm(FormName = "CertificateForm")]
    public CertificateFormModel? Model { get; set; }

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

    /// <summary>
    /// Initializes the component and loads the mail server hostname.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task OnInitializedAsync()
    {
        this.Layout.CurrentStep = "5";
        this.Layout.CompletedSteps.Add("0");
        this.Layout.CompletedSteps.Add("1");
        this.Layout.CompletedSteps.Add("2");
        this.Layout.CompletedSteps.Add("3");
        this.Layout.CompletedSteps.Add("4");
        this.Model ??= new CertificateFormModel();

        // Load mail server hostname from application configuration
        try
        {
            var settings = await appConfigurationService.GetApplicationSettingsAsync();
            this.Model.Domain = settings?.MailServerHostname ?? string.Empty;
        }
        catch (Exception)
        {
            // Configuration not yet set
            this.Model.Domain = string.Empty;
        }
    }

    /// <summary>
    /// Form model for certificate configuration.
    /// </summary>
    public sealed class CertificateFormModel
    {
        /// <summary>
        /// Gets or sets the mail server hostname (domain).
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address for the Let's Encrypt account.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the selected certificate option (skip or letsencrypt).
        /// </summary>
        public string Option { get; set; } = "skip";
    }
}