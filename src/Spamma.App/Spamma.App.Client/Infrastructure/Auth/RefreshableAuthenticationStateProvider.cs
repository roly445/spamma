using System.Net.Http.Json;
using System.Security.Claims;
using MaybeMonad;
using Microsoft.AspNetCore.Components.Authorization;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;

namespace Spamma.App.Client.Infrastructure.Auth;

public class RefreshableAuthenticationStateProvider(
    HttpClient httpClient,
    ILogger<RefreshableAuthenticationStateProvider> logger)
    : AuthenticationStateProvider, IRefreshableAuthenticationStateProvider
{
    private ClaimsPrincipal? _currentUser;

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

    private static ClaimsIdentity CreateClaimsIdentity(UserAuthInfo userInfo)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
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