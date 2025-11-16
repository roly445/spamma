using System.Security.Claims;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Account;

public partial class Passkeys(ICommander commander, IQuerier querier, IJSRuntime jsRuntime, ILogger<Passkeys> logger, AuthenticationStateProvider authenticationStateProvider, INotificationService notificationService) : ComponentBase
{
    private const string WebAuthnUtilsWindowHandle = "WebAuthnUtils";

    private List<PasskeySummary> passkeys = new();
    private bool isLoading = true;
    private bool isRegistering = false;
    private bool isRevoking = false;
    private Guid revokingPasskeyId = Guid.Empty;
    private bool showNameModal;
    private string customPasskeyName = string.Empty;
    private PasskeyFilter filterStatus = PasskeyFilter.All;

    private enum PasskeyFilter
    {
        All,
        Active,
        Revoked,
    }

    protected override async Task OnInitializedAsync()
    {
        await this.LoadPasskeys();
    }

    private async Task LoadPasskeys()
    {
        try
        {
            this.isLoading = true;

            var result = await querier.Send(new GetMyPasskeysQuery());

            if (result?.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                this.passkeys = result.Data.Passkeys.ToList();
            }
            else
            {
                logger.LogWarning("Query failed with status: {Status}", result?.Status);
                notificationService.ShowError("Failed to load your passkeys. Please try again.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading passkeys");
            notificationService.ShowError("An error occurred while loading your passkeys.");
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private List<PasskeySummary> GetFilteredPasskeys()
    {
        return this.filterStatus switch
        {
            PasskeyFilter.Active => this.passkeys.Where(p => !p.IsRevoked).ToList(),
            PasskeyFilter.Revoked => this.passkeys.Where(p => p.IsRevoked).ToList(),
            _ => this.passkeys,
        };
    }

    private void SetFilter(PasskeyFilter filter)
    {
        this.filterStatus = filter;
    }

    private void OpenNameModal()
    {
        this.customPasskeyName = string.Empty;
        this.showNameModal = true;
    }

    private void CloseNameModal()
    {
        this.showNameModal = false;
        this.customPasskeyName = string.Empty;
    }

    private async Task ConfirmAndRegisterPasskey()
    {
        await this.RegisterNewPasskeyWithName(this.customPasskeyName.Trim());
        this.CloseNameModal();
    }

    private void RegisterNewPasskey()
    {
        this.OpenNameModal();
    }

    private async Task RegisterNewPasskeyWithName(string displayName)
    {
        try
        {
            this.isRegistering = true;

            // Get authenticated user information
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                notificationService.ShowError("You must be logged in to register a passkey.");
                return;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                notificationService.ShowError("Unable to retrieve user information. Please try logging in again.");
                return;
            }

            // Check WebAuthn support via interop bridge
            var isSupported = await jsRuntime.InvokeAsync<bool>($"{WebAuthnUtilsWindowHandle}.isWebAuthnSupported");
            if (!isSupported)
            {
                notificationService.ShowError("Your browser does not support passkeys. Please use a modern browser with WebAuthn support.");
                return;
            }

            // Get current domain for rpId (e.g., localhost, example.com)
            var currentDomain = await jsRuntime.InvokeAsync<string>($"{WebAuthnUtilsWindowHandle}.currentHostname");

            // Register the credential using WebAuthn utilities with actual user email and custom display name
            var registrationResponse = await jsRuntime.InvokeAsync<dynamic>(
                $"{WebAuthnUtilsWindowHandle}.registerCredential",
                userEmail,
                displayName,
                userId,
                "Spamma",
                currentDomain);

            // Check if registration was cancelled (null/JsonNull response)
            bool isCancelled = false;

            if (registrationResponse is System.Text.Json.JsonElement jsonElement)
            {
                isCancelled = jsonElement.ValueKind == System.Text.Json.JsonValueKind.Null;
            }
            else if (registrationResponse == null)
            {
                isCancelled = true;
            }

            if (isCancelled)
            {
                notificationService.ShowWarning("Passkey registration was cancelled.");
                return;
            }

            // Extract response data
            string rawId;
            string attestationObjectBase64 = string.Empty;
            try
            {
                if (registrationResponse is System.Text.Json.JsonElement jsonResponse)
                {
                    // Extract rawId
                    if (jsonResponse.TryGetProperty("rawId", out var rawIdProp))
                    {
                        rawId = rawIdProp.GetString() ?? string.Empty;
                    }
                    else
                    {
                        rawId = string.Empty;
                    }

                    // Extract attestationObject from response.attestationObject (already base64 encoded)
                    if (jsonResponse.TryGetProperty("response", out var responseProp) &&
                        responseProp.TryGetProperty("attestationObject", out var attestationProp))
                    {
                        attestationObjectBase64 = attestationProp.GetString() ?? string.Empty;
                    }
                }
                else
                {
                    rawId = registrationResponse?.rawId?.ToString() ?? string.Empty;
                    if (registrationResponse?.response?.attestationObject != null)
                    {
                        attestationObjectBase64 = registrationResponse.response.attestationObject;
                    }
                }

                if (string.IsNullOrEmpty(rawId))
                {
                    notificationService.ShowError("Failed to register passkey: Invalid credential ID.");
                    return;
                }

                if (string.IsNullOrEmpty(attestationObjectBase64))
                {
                    notificationService.ShowError("Failed to register passkey: Invalid attestation object.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error extracting credential data");
                notificationService.ShowError("Failed to register passkey: Invalid response from authenticator.");
                return;
            }

            // Send registration command to backend
            var attestationObject = System.Convert.FromBase64String(attestationObjectBase64);
            var command = new RegisterPasskeyCommand(
                CredentialId: System.Convert.FromBase64String(rawId),
                PublicKey: attestationObject,
                SignCount: 0,
                DisplayName: displayName,
                Algorithm: "-7"); // ES256

            var commandResult = await commander.Send(command);

            if (commandResult?.Status == CommandResultStatus.Succeeded)
            {
                notificationService.ShowSuccess("Security key registered successfully!");
                await this.LoadPasskeys();
            }
            else
            {
                notificationService.ShowError("Failed to register security key. Please try again.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering passkey");
            notificationService.ShowError("An error occurred while registering your security key.");
        }
        finally
        {
            this.isRegistering = false;
        }
    }

    private async Task RevokePasskey(Guid passkeyId)
    {
        try
        {
            this.isRevoking = true;
            this.revokingPasskeyId = passkeyId;

            var command = new RevokePasskeyCommand(PasskeyId: passkeyId);
            var result = await commander.Send(command);

            if (result?.Status == CommandResultStatus.Succeeded)
            {
                notificationService.ShowSuccess("Security key revoked successfully.");
                await this.LoadPasskeys();
            }
            else
            {
                notificationService.ShowError("Failed to revoke security key. Please try again.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking passkey");
            notificationService.ShowError("An error occurred while revoking your security key.");
        }
        finally
        {
            this.isRevoking = false;
            this.revokingPasskeyId = Guid.Empty;
        }
    }
}