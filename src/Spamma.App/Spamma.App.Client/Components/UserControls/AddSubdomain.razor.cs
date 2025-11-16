using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.App.Client.Shared;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.App.Client.Components.UserControls;

public partial class AddSubdomain(ICommander commander, INotificationService notificationService) : ComponentBase
{
    private bool isVisible;
    private bool isAddingSubdomain;
    private Model model = new();
    private DataModel? _dataModel;
    private string? domainName;

    [Parameter]
    public EventCallback OnSubdomainCreated { get; set; }

    public void Open(DataModel dataModel)
    {
        this.model = new Model();
        this._dataModel = dataModel;
        if (this._dataModel.DomainId.HasValue)
        {
            this.model.DomainId = this._dataModel.DomainId.Value;
            this.domainName = this._dataModel.DomainName;
        }

        this.isVisible = true;
    }

    private void Close()
    {
        this.isVisible = false;
        this.model = new Model();
    }

    private async Task HandleAddSubdomain()
    {
        if (this.model.DomainId == Guid.Empty)
        {
            return;
        }

        this.isAddingSubdomain = true;
        this.StateHasChanged();

        try
        {
            var result = await commander.Send(new CreateSubdomainCommand(Guid.NewGuid(), this.model.DomainId,
                this.model.Name, this.model.Description));

            if (result.Status == CommandResultStatus.Succeeded)
            {
                notificationService.ShowSuccess($"Subdomain '{this.model.Name}' created successfully!");
                await this.OnSubdomainCreated.InvokeAsync();
                this.Close();
            }
            else
            {
                notificationService.ShowError("Failed to create subdomain. Please try again.");
            }
        }
        catch (Exception ex)
        {
            notificationService.ShowError($"Error creating subdomain: {ex.Message}");
        }
        finally
        {
            this.isAddingSubdomain = false;
            this.StateHasChanged();
        }
    }

    private void OnDomainChanged(ChangeEventArgs e)
    {
        this.StateHasChanged();
    }

    private string? GetSelectedDomainName()
    {
        if (!string.IsNullOrEmpty(this.domainName))
        {
            return this.domainName;
        }

        if (this._dataModel?.AvailableDomains == null)
        {
            return null;
        }

        var domainId = Guid.NewGuid();
        if (!this._dataModel.DomainId.HasValue)
        {
            if (this.model.DomainId != Guid.Empty)
            {
                domainId = this.model.DomainId;
            }
            else
            {
                return null;
            }
        }
        else
        {
            domainId = this._dataModel.DomainId.Value;
        }

        return this._dataModel.AvailableDomains.FirstOrDefault(d => d.Id == domainId)?.DomainName;
    }

    private sealed class Model
    {
        [Required(ErrorMessage = "Please select a parent domain")]
        [Range(typeof(Guid), "00000000-0000-0000-0000-000000000001", "ffffffff-ffff-ffff-ffff-ffffffffffff",
            ErrorMessage = "Please select a parent domain")]
        public Guid DomainId { get; set; } = Guid.Empty;

        [Required(ErrorMessage = "Subdomain name is required")]
        [RegularExpression(
            @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?$",
            ErrorMessage = "Subdomain name must be 1-63 characters and contain only letters, numbers, and hyphens")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    public record DataModel(Guid? DomainId, string? DomainName, IReadOnlyList<DomainOption>? AvailableDomains);
}