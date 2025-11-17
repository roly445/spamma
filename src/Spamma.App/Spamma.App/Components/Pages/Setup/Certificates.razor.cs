using Microsoft.AspNetCore.Components;
using Spamma.App.Components.Layout;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Components.Pages.Setup;

/// <summary>
/// Code-behind for Certificates setup page.
/// </summary>
public partial class Certificates(IAppConfigurationService appConfigurationService)
{
    [SupplyParameterFromForm(FormName = "CertificateForm")]
    public CertificateFormModel? Model { get; set; }

    [CascadingParameter]
    public SetupLayout Layout { get; set; } = null!;

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

    public sealed class CertificateFormModel
    {
        public string Domain { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Option { get; set; } = "skip";
    }
}