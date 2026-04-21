namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public interface IEmailInboxSettingsService
{
    Task<bool> GetCatchAllModeEnabledAsync(CancellationToken cancellationToken = default);

    Task SetCatchAllModeEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}
