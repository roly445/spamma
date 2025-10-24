using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Auth;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Layout;

public partial class AppLayout(
    IJSRuntime jsRuntime, ISignalRService signalRService, AuthenticationStateProvider authenticationStateProvider) : IDisposable
{
    private bool showSettingsDropdown;

    [JSInvokable]
    public void CloseDropdown()
    {
        this.showSettingsDropdown = false;
        this.StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        signalRService.OnPermissionsUpdated += this.SignalRServiceOnOnPermissionsUpdated;
        await signalRService.StartAsync();
        await base.OnInitializedAsync();
    }

    private Task SignalRServiceOnOnPermissionsUpdated()
    {
        if (authenticationStateProvider is IRefreshableAuthenticationStateProvider refreshableAuthenticationStateProvider)
        {
            return refreshableAuthenticationStateProvider.RefreshAuthenticationStateAsync();
        }

        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.InvokeVoidAsync("addClickOutsideListener", DotNetObjectReference.Create(this));
            await jsRuntime.InvokeVoidAsync("window.hideSplash");
        }
    }

    private void ToggleSettingsDropdown()
    {
        this.showSettingsDropdown = !this.showSettingsDropdown;
    }

    public async void Dispose()
    {
        await signalRService.StopAsync();
    }
}