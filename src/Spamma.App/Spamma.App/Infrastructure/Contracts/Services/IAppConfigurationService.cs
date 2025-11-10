namespace Spamma.App.Infrastructure.Contracts.Services;

public interface IAppConfigurationService
{
    Task SaveEmailSettingsAsync(EmailSettings emailSettings);

    Task<EmailSettings> GetEmailSettingsAsync();

    Task SaveKeysAsync(KeySettings keySettings);

    Task<KeySettings> GetKeySettingsAsync();

    Task SavePrimaryUserAsync(PrimaryUserSettings keySettings);

    Task<PrimaryUserSettings> GetPrimaryUserSettingsAsync();

    Task MarkSetupCompleteAsync();

    Task SaveApplicationSettingsAsync(ApplicationSettings applicationSettings);

    Task<ApplicationSettings> GetApplicationSettingsAsync();

    public record EmailSettings
    {
        public string SmtpHost { get; init; } = string.Empty;

        public int SmtpPort { get; init; } = 587;

        public string? Username { get; init; }

        public string? Password { get; init; }

        public string FromEmail { get; init; } = string.Empty;

        public string FromName { get; init; } = string.Empty;

        public bool UseTls { get; init; }
    }

    public record KeySettings
    {
        public string SigningKey { get; init; } = string.Empty;

        public string JwtKey { get; init; } = string.Empty;
    }

    public record PrimaryUserSettings
    {
        public Guid PrimaryUserId { get; init; } = Guid.Empty;
    }

    public record ApplicationSettings
    {
        public string BaseUrl { get; init; } = string.Empty;

        public string MailServerHostname { get; init; } = string.Empty;

        public int MxPriority { get; init; } = 10;
    }
}