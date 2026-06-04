using BluQube.Constants;
using BluQube.Queries;
using Spamma.App.Client.Infrastructure.Contracts.Services;
using Spamma.Modules.Common.Client.Application.Queries;

namespace Spamma.App.Client.Infrastructure.Services;

public sealed class SystemSettingsCache(IQueryRunner querier, ILogger<SystemSettingsCache> logger) : ISystemSettingsCache
{
    private bool _initialized;

    public event Func<GetSystemSettingsQueryResult, Task>? SettingsChanged;

    public GetSystemSettingsQueryResult Current { get; private set; } = new(false);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._initialized)
        {
            return;
        }

        try
        {
            var result = await querier.Send(new GetSystemSettingsQuery(), cancellationToken);
            if (result.Status == QueryResultStatus.Succeeded)
            {
                this.Current = result.Data;
                this._initialized = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize system settings cache");
        }
    }

    public async Task ApplyAsync(GetSystemSettingsQueryResult settings)
    {
        if (settings == this.Current)
        {
            return;
        }

        this.Current = settings;

        if (this.SettingsChanged is not null)
        {
            await this.SettingsChanged.Invoke(settings);
        }
    }
}
