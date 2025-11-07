using System.Net.Http.Json;
using System.Security.Claims;
using MaybeMonad;
using Microsoft.AspNetCore.Components.Authorization;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;

namespace Spamma.App.Client.Infrastructure.Auth;

/// <summary>
/// Provides authentication state management and refresh capability for Blazor WebAssembly client.
/// </summary>
public class RefreshableAuthenticationStateProvider(
    HttpClient httpClient,
    ILogger<RefreshableAuthenticationStateProvider> logger)
    : AuthenticationStateProvider, IRefreshableAuthenticationStateProvider
{
    private ClaimsPrincipal? _currentUser;

    /// <summary>
    /// Gets the current authentication state asynchronously.
    /// </summary>
    /// <returns>The current authentication state.</returns>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (this._currentUser != null)
        {
            return new AuthenticationState(this._currentUser);
        }

        var maybe = await this.FetchUserAuthInfoAsync();
        AuthenticationState currentAuthenticationState;
        if (maybe.HasNoValue || !maybe.Value.IsAuthenticated)
        {
            this._currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            var identity = CreateClaimsIdentity(maybe.Value);
            this._currentUser = new ClaimsPrincipal(identity);
        }

        currentAuthenticationState = new AuthenticationState(this._currentUser);

        return currentAuthenticationState;
    }

    /// <summary>
    /// Refreshes the authentication state by fetching updated user information from the server.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RefreshAuthenticationStateAsync()
    {
        var maybe = await this.FetchUserAuthInfoAsync();
        if (maybe.HasNoValue || !maybe.Value.IsAuthenticated)
        {
            this._currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            var identity = CreateClaimsIdentity(maybe.Value);
            this._currentUser = new ClaimsPrincipal(identity);
        }

        var currentAuthenticationState = new AuthenticationState(this._currentUser);
        this.NotifyAuthenticationStateChanged(Task.FromResult(currentAuthenticationState));
    }

    /// <summary>
    /// Creates a claims identity from user authentication information.
    /// </summary>
    /// <param name="userInfo">The user authentication information.</param>
    /// <returns>A claims identity populated with user information.</returns>
    private static ClaimsIdentity CreateClaimsIdentity(UserAuthInfo userInfo)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.UserId ?? string.Empty),
            new(ClaimTypes.Name, userInfo.Name ?? string.Empty),
            new(ClaimTypes.Email, userInfo.EmailAddress ?? string.Empty),
            new(ClaimTypes.Role, userInfo.SystemRole.ToString()),
        };

        claims.AddRange(userInfo.ModeratedDomains.Select(domainId =>
            new Claim(Lookups.ModeratedDomainClaim, domainId.ToString())));

        claims.AddRange(userInfo.ModeratedSubdomains.Select(subdomainId =>
            new Claim(Lookups.ModeratedSubdomainClaim, subdomainId.ToString())));

        claims.AddRange(userInfo.ViewableSubdomains.Select(subdomainId =>
            new Claim(Lookups.ViewableSubdomainClaim, subdomainId.ToString())));

        return new ClaimsIdentity(claims, "ServerAuth");
    }

    /// <summary>
    /// Fetches current user authentication information from the server.
    /// </summary>
    /// <returns>User authentication information if available; otherwise, Nothing.</returns>
    private async Task<Maybe<UserAuthInfo>> FetchUserAuthInfoAsync()
    {
        try
        {
            var data = await httpClient.GetFromJsonAsync<UserAuthInfo>("/current-user");
            if (data == null)
            {
                return Maybe<UserAuthInfo>.Nothing;
            }

            return Maybe.From(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get authentication state");
            return Maybe<UserAuthInfo>.Nothing;
        }
    }
}