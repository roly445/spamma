namespace Spamma.App.Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddDatabaseConfiguration(
        this IConfigurationBuilder builder,
        string connectionString,
        ILoggerFactory loggerFactory)
    {
        return builder.Add(new DatabaseConfigurationSource(connectionString, loggerFactory));
    }
}