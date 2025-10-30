namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface ISignalRService
{
    event Func<Task>? OnNewEmailReceived;

    event Func<Task>? OnEmailDeleted;

    event Func<Task>? OnEmailUpdated;

    event Func<Task>? OnPermissionsUpdated;

    bool IsConnected { get; }

    Task StartAsync();

    Task StopAsync();
}