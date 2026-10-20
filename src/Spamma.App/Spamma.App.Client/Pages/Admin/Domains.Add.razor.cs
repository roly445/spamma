using System.ComponentModel.DataAnnotations;
using BluQube.Constants;
using Spamma.App.Client.Infrastructure;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Code-behind for the Domains page.
/// </summary>
public partial class Domains
{
    private readonly List<DnsIntegrationSummary> dnsIntegrations = [];
    private AddDomainModel addDomainModel = new();
    private bool showAddDomainModal;
    private bool isAddingDomain;
    private bool isCheckingVerification;
    private AddDomainStep addDomainStep = AddDomainStep.DomainDetails;
    private string verificationToken = string.Empty;
    private bool isSettingUpDns;
    private string? dnsSetupMessage;
    private string? addDomainError;

    private enum AddDomainStep
    {
        DomainDetails,
        VerificationInstructions,
    }

    private async Task OpenAddDomainModal()
    {
        await domainValidationService.BuildDomainListAsync();
        var integrationsResult = await querier.Send(new ListDnsIntegrationsQuery());
        this.dnsIntegrations.Clear();
        if (integrationsResult.Status == QueryResultStatus.Succeeded)
        {
            this.dnsIntegrations.AddRange(integrationsResult.Data.Integrations);
        }

        this.addDomainModel = new AddDomainModel();
        this.addDomainStep = AddDomainStep.DomainDetails;
        this.addDomainError = null;
        this.showAddDomainModal = true;
    }

    private void CloseAddDomainModal()
    {
        this.showAddDomainModal = false;
        this.addDomainModel = new AddDomainModel();
        this.addDomainStep = AddDomainStep.DomainDetails;
        this.verificationToken = string.Empty;
        this.dnsSetupMessage = null;
        this.addDomainError = null;
    }

    private async Task HandleAddDomain()
    {
        this.isAddingDomain = true;
        this.addDomainError = null;
        this.StateHasChanged();

        var result = await commander.Send(new CreateDomainCommand(
            this.addDomainModel.DomainId, this.addDomainModel.DomainName,
            this.addDomainModel.PrimaryContact, this.addDomainModel.Description, this.addDomainModel.DnsIntegrationId));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            this.verificationToken = $"spamma-verification={result.Data.VerificationToken}";
            this.addDomainStep = AddDomainStep.VerificationInstructions;
            await this.LoadDomains();
        }
        else
        {
            this.addDomainError = "The domain could not be added. Check that the selected DNS integration matches the domain name, or choose manual setup.";
        }

        this.isAddingDomain = false;
        this.StateHasChanged();
    }

    private async Task CheckVerification()
    {
        this.isCheckingVerification = true;
        this.StateHasChanged();

        var result = await commander.Send(new VerifyDomainCommand(this.addDomainModel.DomainId));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"Domain '{this.addDomainModel.DomainName}' verified successfully!");
            this.CloseAddDomainModal();
            await this.LoadDomains();
        }
        else
        {
            notificationService.ShowWarning("Domain verification not yet completed. Please ensure the verification token is correctly set and try again later.");
        }

        this.isCheckingVerification = false;
        this.StateHasChanged();
    }

    private async Task SetupDomainDns()
    {
        this.isSettingUpDns = true;
        try
        {
            var result = await commander.Send(new SetupDomainDnsCommand(this.addDomainModel.DomainId));
            this.dnsSetupMessage = result.Status == CommandResultStatus.Succeeded
                ? result.Data.Message
                : "DNS setup could not be completed. Add the record manually or retry.";
        }
        finally
        {
            this.isSettingUpDns = false;
        }
    }

    private sealed class AddDomainModel
    {
        [Required(ErrorMessage = "Domain name is required.")]
        [RootDomain]
        public string DomainName { get; set; } = string.Empty;

        public Guid? DnsIntegrationId { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? PrimaryContact { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public Guid DomainId { get; } = Guid.NewGuid();
    }
}
