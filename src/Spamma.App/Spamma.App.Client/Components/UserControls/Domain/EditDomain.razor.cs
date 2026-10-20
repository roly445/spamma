using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.App.Client.Components.UserControls.Domain;

/// <summary>
/// Code-behind for the Edit Domain user control.
/// </summary>
public partial class EditDomain(ICommandRunner commander, IQueryRunner querier, INotificationService notificationService) : ComponentBase
{
    private Model model = new();
    private bool isVisible;
    private bool isProcessing;
    private DataModel? _dataModel;
    private IReadOnlyList<DnsIntegrationSummary> dnsIntegrations = [];

    [Parameter]
    public EventCallback OnSaved { get; set; }

    public async Task Open(DataModel dataModel)
    {
        this._dataModel = dataModel;
        this.model = new Model
        {
            PrimaryContact = this._dataModel.PrimaryContact,
            Description = this._dataModel.Description ?? string.Empty,
            DnsIntegrationId = this._dataModel.DnsIntegrationId,
        };
        this.isVisible = true;
        var result = await querier.Send(new ListDnsIntegrationsQuery());
        this.dnsIntegrations = result.Status == QueryResultStatus.Succeeded
            ? result.Data.Integrations.Where(x => string.Equals(x.ZoneName, dataModel.DomainName, StringComparison.OrdinalIgnoreCase) || x.Id == dataModel.DnsIntegrationId).ToList()
            : [];
        this.StateHasChanged();
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

        var result = await commander.Send(new UpdateDomainDetailsCommand(this._dataModel.Id, this.model.PrimaryContact, this.model.Description, this.model.DnsIntegrationId));

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
        int AssignedUserCount,
        Guid? DnsIntegrationId);

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

        public Guid? DnsIntegrationId { get; set; }
    }
}
