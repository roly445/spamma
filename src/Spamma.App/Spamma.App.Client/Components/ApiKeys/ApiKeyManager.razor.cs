using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Components.ApiKeys;

/// <summary>
/// Backing code for ApiKeyManager component.
/// </summary>
public partial class ApiKeyManager(ICommander commander, INotificationService notificationService, IJSRuntime jsRuntime) : ComponentBase
{
    private readonly ICommander _commander = commander;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    private CreateApiKeyModel createModel = new();
    private bool isCreating;
    private string? createdKeyValue;

    private async Task HandleCreateApiKey()
    {
        this.isCreating = true;

        try
        {
            var command = new Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys.CreateApiKeyCommand(
                this.createModel.Name);

            var result = await this._commander.Send(command);

            if (result.Status == CommandResultStatus.Succeeded)
            {
                this.createdKeyValue = result.Data.KeyValue;
                this.createModel = new CreateApiKeyModel(); // Reset form
                this._notificationService.ShowSuccess("API key created successfully!");
            }
            else
            {
                this._notificationService.ShowError("Failed to create API key. Please try again.");
            }
        }
        catch (Exception ex)
        {
            this._notificationService.ShowError($"An error occurred: {ex.Message}");
        }
        finally
        {
            this.isCreating = false;
        }
    }

    private async Task CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(this.createdKeyValue))
        {
            await this._jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", this.createdKeyValue);
            this._notificationService.ShowSuccess("API key copied to clipboard!");
        }
    }

    private sealed class CreateApiKeyModel
    {
        [Required(ErrorMessage = "API key name is required")]
        [StringLength(100, ErrorMessage = "API key name must not exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
    }
}