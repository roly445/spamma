using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.App.Client.Components.UserControls.Domain;

/// <summary>
/// Backing code for the edit domain modal.
/// </summary>
public partial class EditDomain(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private Model model = new();
    private bool isVisible;
    private bool isProcessing;
    private DataModel? _dataModel;

    [Parameter]
    public EventCallback OnSaved { get; set; }

    public void Open(DataModel dataModel)
    {
        this._dataModel = dataModel;
        this.model = new Model
        {
            PrimaryContact = this._dataModel.PrimaryContact,
            Description = this._dataModel.Description ?? string.Empty,
        };
        this.isVisible = true;
    }

    private void Close()
    {
        this.isVisible = false;
        this._dataModel = null;
        this.model = new Model();
    }

    private async Task HandleSaveDomain()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var result = await commander.Send(new UpdateDomainDetailsCommand(this._dataModel.Id, this.model.PrimaryContact, this.model.Description));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess("Domain details updated successfully.");
            await this.OnSaved.InvokeAsync();
            this.Close();
        }
        else
        {
            notificationService.ShowError("Failed to update domain details. Please try again.");
        }

        this.isProcessing = false;
        this.StateHasChanged();
    }

    public record DataModel(
        Guid Id,
        string DomainName,
        string? PrimaryContact,
        string? Description,
        DateTime CreatedAt,
        bool IsVerified,
        DateTime? VerifiedAt,
        int SubdomainCount,
        int AssignedUserCount);

    private sealed class Model
    {
        private string? _primaryContact;

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? PrimaryContact
        {
            get => this._primaryContact;
            set => this._primaryContact = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }
}