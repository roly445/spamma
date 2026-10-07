namespace Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

public interface IArfSmtpSettingsProvider
{
    Task<ArfSmtpSettings> GetAsync();
}
