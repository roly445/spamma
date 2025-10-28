using System.Security.Claims;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Client.Pages.Account;

/// <summary>
/// Backing code for the Security keys management page
/// </summary>
public partial class Security(ICommander commander, IQuerier querier, IJSRuntime jsRuntime, ILogger<Security> logger, AuthenticationStateProvider authenticationStateProvider)
{
    private const string WebAuthnUtilsWindowHandle = "WebAuthnUtils";
    private List<PasskeySummary> passkeys = new();
    private bool isLoading = true;
    private bool isRegistering = false;
    private bool isRevoking = false;
    private Guid revokingPasskeyId = Guid.Empty;
    private string? errorMessage;
    private string? successMessage;

    protected override async Task OnInitializedAsync()
    {
        await this.LoadPasskeys();
    }

    private async Task LoadPasskeys()
    {
        try
        {
            this.isLoading = true;
            this.errorMessage = null;

            var result = await querier.Send(new GetMyPasskeysQuery());

            if (result?.Status == QueryResultStatus.Succeeded && result.Data != null)
            {
                this.passkeys = result.Data.Passkeys.ToList();
            }
            else
            {
                logger.LogWarning("Query failed with status: {Status}", result?.Status);
                this.errorMessage = "Failed to load your security keys. Please try again.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading passkeys");
            this.errorMessage = "An error occurred while loading your security keys.";
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private async Task RegisterNewPasskey()
    {
        try
        {
            this.isRegistering = true;
            this.errorMessage = null;

            // Get authenticated user information
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                this.errorMessage = "You must be logged in to register a security key.";
                return;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                this.errorMessage = "Unable to retrieve user information. Please try logging in again.";
                return;
            }

            // Check WebAuthn support via interop bridge
            var isSupported = await jsRuntime.InvokeAsync<bool>($"{WebAuthnUtilsWindowHandle}.isWebAuthnSupported");
            if (!isSupported)
            {
                this.errorMessage = "Your browser does not support security keys. Please use a modern browser with WebAuthn support.";
                return;
            }

            var displayName = "My Security Key";

            // Get current domain for rpId (e.g., localhost, example.com)
            var currentDomain = await jsRuntime.InvokeAsync<string>($"{WebAuthnUtilsWindowHandle}.currentHostname");

            // Register the credential using WebAuthn utilities with actual user email
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
                this.errorMessage = "Security key registration was cancelled.";
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
                    this.errorMessage = "Failed to register security key: Invalid credential ID.";
                    return;
                }

                if (string.IsNullOrEmpty(attestationObjectBase64))
                {
                    this.errorMessage = "Failed to register security key: Invalid attestation object.";
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error extracting credential data");
                this.errorMessage = "Failed to register security key: Invalid response from authenticator.";
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
                this.successMessage = "Security key registered successfully!";
                await this.LoadPasskeys();

                // Clear success message after 5 seconds
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    this.successMessage = null;
                    this.StateHasChanged();
                });
            }
            else
            {
                this.errorMessage = "Failed to register security key. Please try again.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering passkey");
            this.errorMessage = "An error occurred while registering your security key.";
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
            this.errorMessage = null;
            this.successMessage = null;

            var command = new RevokePasskeyCommand(PasskeyId: passkeyId);
            var result = await commander.Send(command);

            if (result?.Status == CommandResultStatus.Succeeded)
            {
                this.successMessage = "Security key revoked successfully.";
                await this.LoadPasskeys();

                // Clear success message after 5 seconds
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    this.successMessage = null;
                    this.StateHasChanged();
                });
            }
            else
            {
                this.errorMessage = "Failed to revoke security key. Please try again.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking passkey");
            this.errorMessage = "An error occurred while revoking your security key.";
        }
        finally
        {
            this.isRevoking = false;
            this.revokingPasskeyId = Guid.Empty;
        }
    }
}