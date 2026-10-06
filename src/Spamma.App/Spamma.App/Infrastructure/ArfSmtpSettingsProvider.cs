using Spamma.App.Infrastructure.Contracts.Services;
using Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

namespace Spamma.App.Infrastructure;

public sealed class ArfSmtpSettingsProvider(IAppConfigurationService configuration) : IArfSmtpSettingsProvider
{
    public async Task<ArfSmtpSettings> GetAsync()
    {
        var settings = await configuration.GetEmailSettingsAsync();
        return new ArfSmtpSettings(
            settings.SmtpHost,
            settings.SmtpPort,
            settings.Username,
            settings.Password,
            settings.FromEmail,
            settings.FromName,
            settings.UseTls);
    }
}
