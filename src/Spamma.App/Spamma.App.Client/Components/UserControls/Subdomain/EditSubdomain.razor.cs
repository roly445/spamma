using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.App.Client.Components.UserControls.Subdomain;

public partial class EditSubdomain(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private bool isVisible;
    private bool isProcessing;
    private DataModel? _dataModel;
    private Model model = new();

    [Parameter]
    public EventCallback OnSaved { get; set; }

    public void Open(DataModel dataModel)
    {
        this._dataModel = dataModel;
        this.model = new Model
        {
            Description = dataModel.Description,
        };

        this.isVisible = true;
    }

    private void Close()
    {
        this.isVisible = false;
        this._dataModel = null;
        this.model = new Model();
    }

    private async Task HandleEditSubdomain()
    {
        if (this._dataModel == null)
        {
            return;
        }

        this.isProcessing = true;
        this.StateHasChanged();

        var command = new UpdateSubdomainDetailsCommand(
            this._dataModel.Id,
            this.model.Description);
        var result = await commander.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            notificationService.ShowSuccess($"Subdomain '{this._dataModel.FullDomainName}' updated successfully.");
            await this.OnSaved.InvokeAsync();
            this.isVisible = false;
        }
        else
        {
            notificationService.ShowError("Failed to update subdomain. Please try again.");
        }

        this.isProcessing = false;
        this.StateHasChanged();
    }

    private sealed class Model
    {
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public record DataModel(Guid Id, string? Description, string SubdomainName, string DomainName, DateTime CreatedAt)
    {
        public string FullDomainName => $"{this.SubdomainName}.{this.DomainName}";
    }
}