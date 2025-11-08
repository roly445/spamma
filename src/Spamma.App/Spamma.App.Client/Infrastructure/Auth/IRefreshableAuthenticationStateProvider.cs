namespace Spamma.App.Client.Infrastructure.Auth;

public interface IRefreshableAuthenticationStateProvider
{
    Task RefreshAuthenticationStateAsync();
}