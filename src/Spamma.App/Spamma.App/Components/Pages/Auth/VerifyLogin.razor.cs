using System.Security.Claims;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Spamma.App.Infrastructure.Services;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.App.Components.Pages.Auth;

/// <summary>
/// Backing code for the VerifyLogin component, which handles magic link authentication verification.
/// </summary>
public partial class VerifyLogin(
    ICommander commander,
    NavigationManager navigation,
    ILogger<VerifyLogin> logger, IAuthTokenProvider authTokenProvider,
    IHttpContextAccessor httpContextAccessor, IQuerier querier,
    UserStatusCache userStatusCache, IInternalQueryStore internalQueryStore) : ComponentBase
{
    private string? errorMessage;

    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    [SupplyParameterFromForm(FormName = "VerifyLoginForm")]
    public VerifyLoginModel Model { get; set; } = new();

    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(this.Token))
        {
            logger.LogWarning("Magic link verification attempted without token");
            this.errorMessage = "The authentication link is invalid or has expired.";
        }
        else
        {
            this.Model.Token = this.Token;
        }
    }

    private async Task HandleVerification()
    {
        // Validate required parameters
        if (string.IsNullOrEmpty(this.Token))
        {
            logger.LogWarning("Magic link verification attempted without token");
            this.errorMessage = "The authentication link is invalid or has expired.";
            return;
        }

        logger.LogInformation("Processing magic link verification for token: {TokenPrefix}...", this.Token[..Math.Min(8, this.Token.Length)]);

        var tokenResult = authTokenProvider.ProcessAuthenticationToken(this.Token);
        if (tokenResult.IsFailure)
        {
            logger.LogWarning("Magic link token processing failed");
            this.errorMessage = "The authentication link is invalid or has expired.";
            return;
        }

        // Verify the magic link token
        var verifyCommand = new CompleteAuthenticationCommand(tokenResult.Value.UserId, tokenResult.Value.SecurityStamp, tokenResult.Value.AuthenticationAttemptId);
        var result = await commander.Send(verifyCommand);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            var httpContext = httpContextAccessor.HttpContext;
            await this.SignInUser(httpContext!, tokenResult.Value.UserId);

            // Successful authentication - redirect to int.ended destination
            navigation.NavigateTo("/app", forceLoad: true);
        }
        else
        {
            logger.LogWarning("Magic link verification failed - {Reason}", result.ErrorData.Message);
            this.errorMessage = "The authentication link is invalid or has expired.";
        }
    }

    private async Task SignInUser(HttpContext httpContext, Guid userId)
    {
        // Create claims for the authenticated user
        var query = new GetUserByIdQuery(userId);
        internalQueryStore.StoreQueryRef(query);
        var userResult = await querier.Send(query);
        var claims = ClaimsBuilder.BuildClaims(
            userResult.Data.Id,
            userResult.Data.EmailAddress,
            userResult.Data.Name,
            userResult.Data.SystemRole,
            userResult.Data.ModeratedDomains,
            userResult.Data.ModeratedSubdomains,
            userResult.Data.ViewableSubdomains);

        // Create claims identity
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Create authentication properties
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Remember user across browser sessions
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30), // 30-day expiration
            AllowRefresh = true,
        };

        await userStatusCache.SetUserLookupAsync(new UserStatusCache.CachedUser
        {
            IsSuspended = userResult.Data.IsSuspended,
            ModeratedDomains = userResult.Data.ModeratedDomains.ToList(),
            ModeratedSubdomains = userResult.Data.ModeratedSubdomains.ToList(),
            SystemRole = userResult.Data.SystemRole,
            EmailAddress = userResult.Data.EmailAddress,
            Name = userResult.Data.Name,
            UserId = userResult.Data.Id,
        });

        // Sign in the user (this creates the authentication cookie)
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties);

        logger.LogInformation("User {UserId} successfully signed in via magic link", userId);
    }

    public sealed class VerifyLoginModel
    {
        public string Token { get; set; } = string.Empty;
    }
}