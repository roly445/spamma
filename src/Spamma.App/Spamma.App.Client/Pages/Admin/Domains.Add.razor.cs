using System.ComponentModel.DataAnnotations;
using BluQube.Constants;
using Spamma.App.Client.Infrastructure;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the domains management page - add domain functionality.
/// </summary>
public partial class Domains
{
    private AddDomainModel addDomainModel = new();
    private bool showAddDomainModal;
    private bool isAddingDomain;
    private bool isCheckingVerification;
    private AddDomainStep addDomainStep = AddDomainStep.DomainDetails;
    private string verificationToken = string.Empty;

    private enum AddDomainStep
    {
        DomainDetails,
        VerificationInstructions,
    }

    private async Task OpenAddDomainModal()
    {
        await domainValidationService.BuildDomainListAsync();
        this.addDomainModel = new AddDomainModel();
        this.addDomainStep = AddDomainStep.DomainDetails;
        this.showAddDomainModal = true;
    }

    private void CloseAddDomainModal()
    {
        this.showAddDomainModal = false;
        this.addDomainModel = new AddDomainModel();
        this.addDomainStep = AddDomainStep.DomainDetails;
        this.verificationToken = string.Empty;
    }

    private async Task HandleAddDomain()
    {
        this.isAddingDomain = true;
        this.StateHasChanged();

        var result = await commander.Send(new CreateDomainCommand(
            this.addDomainModel.DomainId, this.addDomainModel.DomainName,
            this.addDomainModel.PrimaryContact, this.addDomainModel.Description));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            this.verificationToken = $"spamma-verification={result.Data.VerificationToken}";
            this.addDomainStep = AddDomainStep.VerificationInstructions;
            await this.LoadDomains();
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

    private sealed class AddDomainModel
    {
        [Required(ErrorMessage = "Domain name is required.")]
        [RootDomain]
        public string DomainName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? PrimaryContact { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public Guid DomainId { get; } = Guid.NewGuid();
    }
}