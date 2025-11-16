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
    private bool showRevokeModal;
    private Guid? revokeApiKeyId;
    private string? revokeApiKeyName;
    private string selectedExpiry = "never";
    private string? customExpiryDate;
    private KeyFilter filterStatus = KeyFilter.All;

    private enum KeyFilter
    {
        All,
        Active,
        Expired,
        Revoked,
    }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadApiKeys();
    }

    private static bool IsExpired(ApiKeySummary apiKey)
    {
        return apiKey.WhenExpires != DateTimeOffset.MaxValue && DateTimeOffset.UtcNow > apiKey.WhenExpires;
    }

    private static string GetKeyBackgroundColor(ApiKeySummary apiKey)
    {
        if (apiKey.IsRevoked)
        {
            return "bg-gray-50";
        }

        if (IsExpired(apiKey))
        {
            return "bg-yellow-50";
        }

        return "bg-white";
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

            // Set the expiry date based on selection
            command.WhenExpires = this.CalculateExpiryDate();

            var result = await commander.Send(command);

            if (result.Status == CommandResultStatus.Succeeded)
            {
                this.createdKeyValue = result.Data.KeyValue;
                this.createModel = new CreateApiKeyModel(); // Reset form
                this.selectedExpiry = "never"; // Reset expiry selection
                this.customExpiryDate = null; // Reset custom date
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

    private DateTimeOffset CalculateExpiryDate()
    {
        return this.selectedExpiry switch
        {
            "30" => DateTimeOffset.UtcNow.AddDays(30),
            "90" => DateTimeOffset.UtcNow.AddDays(90),
            "180" => DateTimeOffset.UtcNow.AddDays(180),
            "365" => DateTimeOffset.UtcNow.AddDays(365),
            "custom" when !string.IsNullOrEmpty(this.customExpiryDate) && DateTime.TryParse(this.customExpiryDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var customDate) =>
                new DateTimeOffset(customDate, TimeSpan.Zero),
            _ => DateTimeOffset.MaxValue, // "never" - set to max date
        };
    }

    private async Task OpenCreateModal()
    {
        this.showCreateModal = true;
        this.selectedExpiry = "never"; // Reset to default
        this.customExpiryDate = null;

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

    private Task HandleRevokeApiKey(Guid apiKeyId)
    {
        // Open the confirmation modal instead of using a JS confirm dialog
        var apiKey = this.apiKeys.FirstOrDefault(k => k.Id == apiKeyId);
        if (apiKey == null)
        {
            notificationService.ShowError("API key not found.");
            return Task.CompletedTask;
        }

        this.revokeApiKeyId = apiKeyId;
        this.revokeApiKeyName = apiKey.Name;
        this.showRevokeModal = true;
        return Task.CompletedTask;
    }

    private void CloseRevokeModal()
    {
        this.showRevokeModal = false;
        this.revokeApiKeyId = null;
        this.revokeApiKeyName = null;
    }

    private async Task ConfirmRevokeApiKey()
    {
        if (this.revokeApiKeyId == null)
        {
            notificationService.ShowError("No API key selected to revoke.");
            this.showRevokeModal = false;
            return;
        }

        var apiKeyId = this.revokeApiKeyId.Value;
        this.isRevoking = true;
        this.showRevokeModal = false; // close the modal while performing the action
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
            this.revokeApiKeyId = null;
            this.revokeApiKeyName = null;
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

    private void SetFilter(KeyFilter filter)
    {
        this.filterStatus = filter;
    }

    private List<ApiKeySummary> GetFilteredApiKeys()
    {
        return this.filterStatus switch
        {
            KeyFilter.Active => this.apiKeys.Where(k => !k.IsRevoked && !IsExpired(k)).ToList(),
            KeyFilter.Expired => this.apiKeys.Where(k => !k.IsRevoked && IsExpired(k)).ToList(),
            KeyFilter.Revoked => this.apiKeys.Where(k => k.IsRevoked).ToList(),
            _ => this.apiKeys,
        };
    }

    private sealed class CreateApiKeyModel
    {
        [Required(ErrorMessage = "API key name is required")]
        [StringLength(100, ErrorMessage = "API key name must not exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
    }
}