using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Spamma.App.Client.Infrastructure.Auth;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Layout;

public partial class AppLayout(
    IJSRuntime jsRuntime, ISignalRService signalRService, AuthenticationStateProvider authenticationStateProvider) : IDisposable
{
    private bool showSettingsDropdown;
    private bool _disposed;
    private string? outsideClickHandlerId;
    private DotNetObjectReference<AppLayout>? dotNetRef;

    [JSInvokable]
    public void CloseDropdown()
    {
        this.showSettingsDropdown = false;
        this.StateHasChanged();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            signalRService.StopAsync().Wait();
        }

        this._disposed = true;
    }

    protected override async Task OnInitializedAsync()
    {
        signalRService.OnPermissionsUpdated += this.SignalRServiceOnOnPermissionsUpdated;
        await signalRService.StartAsync();
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.InvokeVoidAsync("addClickOutsideListener", DotNetObjectReference.Create(this));
            await jsRuntime.InvokeVoidAsync("window.hideSplash");
        }

        // Register outside-click handler when dropdown opens
        if (this.showSettingsDropdown && string.IsNullOrEmpty(this.outsideClickHandlerId))
        {
            try
            {
                this.dotNetRef = DotNetObjectReference.Create(this);
                this.outsideClickHandlerId = await jsRuntime.InvokeAsync<string>("registerOutsideClickHandler", ".settings-dropdown-container", this.dotNetRef, "CloseDropdown");
            }
            catch
            {
                // Non-fatal if JS handler fails
            }
        }

        // Clean up outside-click handler when dropdown closes
        if (!this.showSettingsDropdown && !string.IsNullOrEmpty(this.outsideClickHandlerId))
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("removeOutsideClickHandler", this.outsideClickHandlerId);
                this.outsideClickHandlerId = null;
                this.dotNetRef?.Dispose();
                this.dotNetRef = null;
            }
            catch
            {
                // Non-fatal if cleanup fails
            }
        }
    }

    private Task SignalRServiceOnOnPermissionsUpdated()
    {
        if (authenticationStateProvider is IRefreshableAuthenticationStateProvider refreshableAuthenticationStateProvider)
        {
            return refreshableAuthenticationStateProvider.RefreshAuthenticationStateAsync();
        }

        return Task.CompletedTask;
    }

    private void ToggleSettingsDropdown()
    {
        this.showSettingsDropdown = !this.showSettingsDropdown;
    }
}