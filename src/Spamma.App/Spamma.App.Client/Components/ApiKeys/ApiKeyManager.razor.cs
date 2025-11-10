using System.ComponentModel.DataAnnotations;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;
using Spamma.Modules.UserManagement.Client.Application.DTOs;
using Spamma.Modules.UserManagement.Client.Application.Queries.ApiKeys;

namespace Spamma.App.Client.Components.ApiKeys;

/// <summary>
/// Backing code for ApiKeyManager component.
/// </summary>
public partial class ApiKeyManager(ICommander commander, IQuerier querier, INotificationService notificationService, IJSRuntime jsRuntime) : ComponentBase
{
    private readonly ICommander _commander = commander;
    private readonly IQuerier _querier = querier;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    private CreateApiKeyModel createModel = new();
    private bool isCreating;
    private string? createdKeyValue;
    private List<ApiKeySummary> apiKeys = new();
    private bool isLoadingKeys;
    private bool isRevoking;

    protected override async Task OnInitializedAsync()
    {
        await this.LoadApiKeys();
    }

    private async Task LoadApiKeys()
    {
        this.isLoadingKeys = true;

        try
        {
            var query = new GetMyApiKeysQuery();
            var result = await this._querier.Send(query);

            if (result.Status == QueryResultStatus.Succeeded)
            {
                this.apiKeys = result.Data.ApiKeys.ToList();
            }
            else
            {
                this._notificationService.ShowError("Failed to load API keys.");
            }
        }
        catch (Exception ex)
        {
            this._notificationService.ShowError($"An error occurred: {ex.Message}");
        }
        finally
        {
            this.isLoadingKeys = false;
        }
    }

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
                await this.LoadApiKeys(); // Refresh the list
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

    private async Task HandleRevokeApiKey(Guid apiKeyId)
    {
        var apiKey = this.apiKeys.FirstOrDefault(k => k.Id == apiKeyId);
        if (apiKey == null)
        {
            this.NotificationService.ShowError("API key not found.");
            return;
        }

        var confirmed = await this.JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Are you sure you want to revoke the API key '{apiKey.Name}'? This action cannot be undone.");

        if (!confirmed)
        {
            return;
        }

        this.isRevoking = true;
        try
        {
            var command = new RevokeApiKeyCommand(apiKeyId);
            var result = await this.Commander.Send(command);

            if (result.Status == CommandResultStatus.Succeeded)
            {
                this.NotificationService.ShowSuccess("API key revoked successfully.");
                await this.LoadApiKeys();
            }
            else
            {
                this.NotificationService.ShowError("Failed to revoke API key. Please try again.");
            }
        }
        catch (Exception ex)
        {
            this.NotificationService.ShowError($"An error occurred: {ex.Message}");
        }
        finally
        {
            this.isRevoking = false;
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