namespace Spamma.App.Client.Infrastructure.Auth;

/// <summary>
/// Provides authentication state refresh capability for the Blazor WebAssembly client.
/// </summary>
public interface IRefreshableAuthenticationStateProvider
{
    /// <summary>
    /// Refreshes the authentication state by fetching updated user information.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshAuthenticationStateAsync();
}