using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Components.UserControls.Subdomain;

/// <summary>
/// Bcking code for the suspend subdomain modal.
/// </summary>
public partial class SuspendSubdomain(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private bool isVisible;
    private bool isProcessing = false;
    private DataModel? _dataModel;
    private Model model = new();

    [Parameter]
    public EventCallback OnSuspended { get; set; }

    public void Open(DataModel dataModel)
    {
        this._dataModel = dataModel;
        this.model = new Model();
        this.isVisible = true;
    }

    private void Close()
    {
        this.isVisible = false;
        this._dataModel = null;
        this.model = new Model();
    }

    private async Task HandleSuspendSubdomain()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var result = await commander.Send(new SuspendSubdomainCommand(this._dataModel.Id, this.model.Reason,
            this.model.Notes));

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"Subdomain '{this._dataModel.FullName}' suspended successfully.");
            await this.OnSuspended.InvokeAsync();
            this.Close();
        }
        else
        {
            notificationService.ShowError($"Subdomain '{this._dataModel.FullName}' failed to suspend.");
        }

        this.isProcessing = false;
        this.StateHasChanged();
    }

    private sealed class Model
    {
        [Required(ErrorMessage = "Please select a suspension reason")]
        public SubdomainSuspensionReason Reason { get; set; } = SubdomainSuspensionReason.AdminRequest;

        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }

    public record DataModel(Guid Id, string FullName);
}