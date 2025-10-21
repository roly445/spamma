namespace Spamma.App.Infrastructure.Configuration;

public class DatabaseConfigurationSource(string connectionString, ILoggerFactory loggerFactory)
    : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var logger = loggerFactory.CreateLogger<DatabaseConfigurationProvider>();
        return new DatabaseConfigurationProvider(connectionString, logger);
    }
}