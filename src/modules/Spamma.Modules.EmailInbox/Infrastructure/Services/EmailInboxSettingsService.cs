using Marten;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

internal class EmailInboxSettingsService(IDocumentSession documentSession) : IEmailInboxSettingsService
{
    public async Task<bool> GetCatchAllModeEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await documentSession.LoadAsync<EmailInboxSettingsDocument>(EmailInboxSettingsDocument.SettingsId, cancellationToken);
        return settings?.CatchAllModeEnabled ?? false;
    }

    public async Task SetCatchAllModeEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var settings = await documentSession.LoadAsync<EmailInboxSettingsDocument>(EmailInboxSettingsDocument.SettingsId, cancellationToken)
            ?? new EmailInboxSettingsDocument();
        settings.CatchAllModeEnabled = enabled;
        documentSession.Store(settings);
        await documentSession.SaveChangesAsync(cancellationToken);
    }
}
