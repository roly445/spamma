using Spamma.Modules.Common.Client.Application.Queries;

namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface ISignalRService
{
    event Func<Task>? OnNewEmailReceived;

    event Func<Task>? OnCatchAllEmailReceived;

    event Func<Task>? OnEmailDeleted;

    event Func<Task>? OnEmailUpdated;

    event Func<Task>? OnPermissionsUpdated;

    event Func<GetSystemSettingsQueryResult, Task>? OnSystemSettingsUpdated;

    bool IsConnected { get; }

    Task StartAsync();

    Task StopAsync();
}
