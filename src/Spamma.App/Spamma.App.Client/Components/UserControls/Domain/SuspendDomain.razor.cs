using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.App.Client.Components.UserControls.Domain;

/// <summary>
/// Code-behind for the SuspendDomain component.
/// </summary>
public partial class SuspendDomain(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private Model model = new();
    private bool isVisible;
    private bool isProcessing;
    private DataModel? _dataModel;

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
        this.model = new Model();
    }

    private async Task HandleSuspendDomain()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var result = await commander.Send(new SuspendDomainCommand(this._dataModel.DomainId, this.model.Reason,
            this.model.Notes));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"Domain '{this._dataModel.DomainName}' suspended successfully.");
            await this.OnSuspended.InvokeAsync();
            this.Close();
        }
        else
        {
            notificationService.ShowError("Failed to suspend domain. Please try again.");
            Console.WriteLine($"Error suspending domain:");
        }

        this.isProcessing = false;
        this.StateHasChanged();
    }

    public record DataModel(Guid DomainId, string DomainName);

    private sealed class Model
    {
        [Required(ErrorMessage = "Please select a reason for suspension.")]
        public DomainSuspensionReason Reason { get; set; } = DomainSuspensionReason.Other;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }
}