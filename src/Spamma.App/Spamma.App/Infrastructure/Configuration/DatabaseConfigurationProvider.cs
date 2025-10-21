using Dapper;
using Npgsql;

namespace Spamma.App.Infrastructure.Configuration;

public class DatabaseConfigurationProvider(string connectionString, ILogger<DatabaseConfigurationProvider> logger)
    : ConfigurationProvider
{
    public override void Load()
    {
        try
        {
            this.EnsureTableExists();
            this.LoadFromDatabase();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load configuration from database, using defaults");
        }
    }

    private static string ConvertToConfigurationKey(string databaseKey)
    {
        return databaseKey switch
        {
            "setup.completed" => "Setup:Completed",
            "setup.version" => "Setup:Version",
            "smtp.host" => "Settings:EmailSmtpHost",
            "smtp.port" => "Settings:EmailSmtpPort",
            "smtp.username" => "Settings:EmailSmtpUsername",
            "smtp.password" => "Settings:EmailSmtpPassword",
            "from.email" => "Settings:FromEmailAddress",
            "from.name" => "Settings:FromName",
            "security.signingKey" => "Settings:SigningKeyBase64",
            "security.jwtKey" => "Settings:JwtKey",
            "primaryuser.id" => "Settings:PrimaryUserId",
            "application.baseUrl" => "Settings:BaseUri",
            "application.mailServerHostname" => "Settings:MailServerHostname",
            "application.mxPriority" => "Settings:MxPriority",
            _ => $"Settings:{databaseKey.Replace(".", ":")}",
        };
    }

    private void EnsureTableExists()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var createTableSql = @"
                CREATE TABLE IF NOT EXISTS app_configuration (
                    id SERIAL PRIMARY KEY,
                    key VARCHAR(255) UNIQUE NOT NULL,
                    value TEXT NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE
                );

                CREATE INDEX IF NOT EXISTS idx_app_configuration_key ON app_configuration(key);";

        connection.Execute(createTableSql);
    }

    private void LoadFromDatabase()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var configurations = connection.Query<(string Key, string Value)>(
            "SELECT key, value FROM app_configuration");

        foreach (var (key, value) in configurations)
        {
            var configKey = ConvertToConfigurationKey(key);
            data[configKey] = value;
        }

        this.Data = data;
        logger.LogInformation("Loaded {Count} configuration values from database", data.Count);
    }
}