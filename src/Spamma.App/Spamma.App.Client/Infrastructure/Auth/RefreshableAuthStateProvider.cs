using System.Net.Http.Json;
using System.Security.Claims;
using MaybeMonad;
using Microsoft.AspNetCore.Components.Authorization;
using Spamma.Modules.Common.Client;

namespace Spamma.App.Client.Infrastructure.Auth;

public interface IRefreshableAuthenticationStateProvider
{
    Task RefreshAuthenticationStateAsync();
}

public class RefreshableAuthenticationStateProvider(
    HttpClient httpClient,
    ILogger<RefreshableAuthenticationStateProvider> logger)
    : AuthenticationStateProvider, IRefreshableAuthenticationStateProvider
{
    private AuthenticationState _currentAuthenticationState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var maybe = await this.FetchUserAuthInfoAsync();
        if (maybe.HasNoValue || !maybe.Value.IsAuthenticated)
        {
            this._currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = CreateClaimsIdentity(maybe.Value);
        this._currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal(identity));

        return this._currentAuthenticationState;
    }

    public async Task RefreshAuthenticationStateAsync()
    {
        ClaimsPrincipal user;

        var maybe = await this.FetchUserAuthInfoAsync();
        if (maybe.HasNoValue || !maybe.Value.IsAuthenticated)
        {
            user = new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            var identity = CreateClaimsIdentity(maybe.Value);
            user = new ClaimsPrincipal(identity);
        }

        this._currentAuthenticationState = new AuthenticationState(user);
        this.NotifyAuthenticationStateChanged(Task.FromResult(this._currentAuthenticationState));
    }

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