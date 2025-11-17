using Dapper;
using Npgsql;
using Spamma.App.Infrastructure.Contracts.Services;

namespace Spamma.App.Infrastructure.Services;

public class AppConfigurationService(IConfiguration configuration, ILogger<AppConfigurationService> logger)
    : IAppConfigurationService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;

    public async Task SaveEmailSettingsAsync(IAppConfigurationService.EmailSettings emailSettings)
    {
        await this.SetValueAsync("smtp.host", emailSettings.SmtpHost);
        await this.SetValueAsync("smtp.port", emailSettings.SmtpPort.ToString());
        await this.SetValueAsync("from.email", emailSettings.FromEmail);
        await this.SetValueAsync("from.name", emailSettings.FromName);
        await this.SetValueAsync("smtp.useTls", emailSettings.UseTls ? "true" : "false");

        if (!string.IsNullOrEmpty(emailSettings.Username))
        {
            await this.SetValueAsync("smtp.username", emailSettings.Username);
        }

        if (!string.IsNullOrEmpty(emailSettings.Password))
        {
            await this.SetValueAsync("smtp.password", emailSettings.Password);
        }
    }

    public async Task<IAppConfigurationService.EmailSettings> GetEmailSettingsAsync()
    {
        return new IAppConfigurationService.EmailSettings
        {
            SmtpHost = await this.GetValueAsync("smtp.host") ?? string.Empty,
            SmtpPort = int.TryParse(await this.GetValueAsync("smtp.port"), out var port) ? port : 587,
            Username = await this.GetValueAsync("smtp.username"),
            Password = await this.GetValueAsync("smtp.password"),
            FromEmail = await this.GetValueAsync("from.email") ?? string.Empty,
            FromName = await this.GetValueAsync("from.name") ?? string.Empty,
            UseTls = (await this.GetValueAsync("smtp.useTls"))?.ToLower() == "true",
        };
    }

    public async Task SaveKeysAsync(IAppConfigurationService.KeySettings keySettings)
    {
        await this.SetValueAsync("security.signingKey", keySettings.SigningKey);
    }

    public async Task<IAppConfigurationService.KeySettings> GetKeySettingsAsync()
    {
        return new IAppConfigurationService.KeySettings
        {
            SigningKey = await this.GetValueAsync("security.signingKey") ?? string.Empty,
        };
    }

    public Task SavePrimaryUserAsync(IAppConfigurationService.PrimaryUserSettings keySettings)
    {
        return this.SetValueAsync("primaryuser.id", keySettings.PrimaryUserId.ToString());
    }

    public async Task<IAppConfigurationService.PrimaryUserSettings> GetPrimaryUserSettingsAsync()
    {
        return new IAppConfigurationService.PrimaryUserSettings
        {
            PrimaryUserId = Guid.Parse(await this.GetValueAsync("primaryuser.id") ?? Guid.Empty.ToString()),
        };
    }

    public async Task MarkSetupCompleteAsync()
    {
        await this.SetValueAsync("setup.completed", DateTime.UtcNow.ToString("O"));
        await this.SetValueAsync("setup.version", "1.0");
    }

    public async Task SaveApplicationSettingsAsync(IAppConfigurationService.ApplicationSettings applicationSettings)
    {
        await this.SetValueAsync("application.baseUrl", applicationSettings.BaseUrl);
        await this.SetValueAsync("application.mailServerHostname", applicationSettings.MailServerHostname);
        await this.SetValueAsync("application.mxPriority", applicationSettings.MxPriority.ToString());
    }

    public async Task<IAppConfigurationService.ApplicationSettings> GetApplicationSettingsAsync()
    {
        return new IAppConfigurationService.ApplicationSettings
        {
            BaseUrl = await this.GetValueAsync("application.baseUrl") ?? string.Empty,
            MailServerHostname = await this.GetValueAsync("application.mailServerHostname") ?? string.Empty,
            MxPriority = int.Parse(await this.GetValueAsync("application.mxPriority") ?? "10"),
        };
    }

    private async Task<string?> GetValueAsync(string key)
    {
        await using var connection = new NpgsqlConnection(this._connectionString);
        return await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT value FROM app_configuration WHERE key = @key",
            new { key });
    }

    private async Task SetValueAsync(string key, string value)
    {
        await using var connection = new NpgsqlConnection(this._connectionString);

        var sql = @"
            INSERT INTO app_configuration (key, value, created_at) 
            VALUES (@key, @value, @now)
            ON CONFLICT (key) 
            DO UPDATE SET value = @value, updated_at = @now";

        await connection.ExecuteAsync(sql, new { key, value, now = DateTime.UtcNow });

        logger.LogInformation("Configuration value '{Key}' updated", key);
    }
}