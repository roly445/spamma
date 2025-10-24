using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Spamma.App.Client.Infrastructure.Contracts.Services;

namespace Spamma.App.Client.Infrastructure.Services;

public class SignalRService(
    NavigationManager navigationManager,
    AuthenticationStateProvider authStateProvider,
    ILogger<SignalRService> logger)
    : ISignalRService, IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event Func<Task>? OnNewEmailReceived;

    public event Func<Task>? OnEmailDeleted;

    public event Func<Task>? OnEmailUpdated;

    public event Func<Task>? OnPermissionsUpdated;

    public bool IsConnected =>
        this._hubConnection?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (this._hubConnection is not null)
        {
            return;
        }

        this._hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/emailhub"))
            .WithAutomaticReconnect(
            [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)])
            .Build();

        // Register event handlers
        this._hubConnection.On("NewEmailReceived", async () =>
        {
            if (this.OnNewEmailReceived is not null)
            {
                await this.OnNewEmailReceived.Invoke();
            }
        });

        this._hubConnection.On("EmailDeleted", async () =>
        {
            if (this.OnEmailDeleted is not null)
            {
                await this.OnEmailDeleted.Invoke();
            }
        });

        this._hubConnection.On("EmailUpdated", async () =>
        {
            if (this.OnEmailUpdated is not null)
            {
                await this.OnEmailUpdated.Invoke();
            }
        });

        this._hubConnection.Reconnected += async (connectionId) =>
        {
            logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            await this.JoinUserGroup();
        };

        this._hubConnection.Closed += (error) =>
        {
            if (error != null)
            {
                logger.LogError(error, "SignalR connection closed with error");
            }
            else
            {
                logger.LogInformation("SignalR connection closed");
            }

            return Task.CompletedTask;
        };

        try
        {
            await this._hubConnection.StartAsync();
            await this.JoinUserGroup();
            logger.LogInformation("SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start SignalR connection");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (this._hubConnection is not null)
        {
            await this._hubConnection.StopAsync();
            await this._hubConnection.DisposeAsync();
            this._hubConnection = null;
        }
    }

    private async Task JoinUserGroup()
    {
        if (this._hubConnection?.State == HubConnectionState.Connected)
        {
            var userId = await this.GetCurrentUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                await this._hubConnection.SendAsync("JoinUserGroup", userId);
            }
        }
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        try
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            if (authState.User?.Identity?.IsAuthenticated == true)
            {
                return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current user ID");
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await this.StopAsync();
    }
}