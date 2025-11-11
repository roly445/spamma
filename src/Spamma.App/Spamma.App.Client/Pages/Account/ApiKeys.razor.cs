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

namespace Spamma.App.Client.Pages.Account;

public partial class ApiKeys(ICommander commander, IQuerier querier, INotificationService notificationService, IJSRuntime jsRuntime) : ComponentBase
{
    private CreateApiKeyModel createModel = new();
    private bool isCreating;
    private bool showCreateModal;
    private ElementReference nameInputRef;
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
            var result = await querier.Send(query);

            if (result.Status == QueryResultStatus.Succeeded)
            {
                this.apiKeys = result.Data.ApiKeys.ToList();
            }
            else
            {
                notificationService.ShowError("Failed to load API keys.");
            }
        }
        catch (Exception ex)
        {
            notificationService.ShowError($"An error occurred: {ex.Message}");
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

            var result = await commander.Send(command);

            if (result.Status == CommandResultStatus.Succeeded)
            {
                this.createdKeyValue = result.Data.KeyValue;
                this.createModel = new CreateApiKeyModel(); // Reset form
                notificationService.ShowSuccess("API key created successfully!");
                await this.LoadApiKeys(); // Refresh the list
                this.showCreateModal = false;
            }
            else
            {
                notificationService.ShowError("Failed to create API key. Please try again.");
            }
        }
        catch (Exception ex)
        {
            notificationService.ShowError($"An error occurred: {ex.Message}");
        }
        finally
        {
            this.isCreating = false;
        }
    }

    private async Task OpenCreateModal()
    {
        this.showCreateModal = true;

        // Defer focus until the modal is rendered
        await Task.Yield();
        try
        {
            await this.nameInputRef.FocusAsync();
        }
        catch
        {
            // Swallow focus exceptions - it's a nice-to-have, not critical
        }
    }

    private void CloseCreateModal()
    {
        this.showCreateModal = false;
    }

    private async Task HandleRevokeApiKey(Guid apiKeyId)
    {
        var apiKey = this.apiKeys.FirstOrDefault(k => k.Id == apiKeyId);
        if (apiKey == null)
        {
            notificationService.ShowError("API key not found.");
            return;
        }

        var confirmed = await jsRuntime.InvokeAsync<bool>(
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
            var result = await commander.Send(command);

            if (result.Status == CommandResultStatus.Succeeded)
            {
                notificationService.ShowSuccess("API key revoked successfully.");
                await this.LoadApiKeys();
            }
            else
            {
                notificationService.ShowError("Failed to revoke API key. Please try again.");
            }
        }
        catch (Exception ex)
        {
            notificationService.ShowError($"An error occurred: {ex.Message}");
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
            await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", this.createdKeyValue);
            notificationService.ShowSuccess("API key copied to clipboard!");
        }
    }

    private sealed class CreateApiKeyModel
    {
        [Required(ErrorMessage = "API key name is required")]
        [StringLength(100, ErrorMessage = "API key name must not exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
    }
}