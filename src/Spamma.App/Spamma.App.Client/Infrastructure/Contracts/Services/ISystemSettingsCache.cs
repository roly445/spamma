using Spamma.Modules.Common.Client.Application.Queries;

namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface ISystemSettingsCache
{
    event Func<GetSystemSettingsQueryResult, Task>? SettingsChanged;

    GetSystemSettingsQueryResult Current { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ApplyAsync(GetSystemSettingsQueryResult settings);
}
