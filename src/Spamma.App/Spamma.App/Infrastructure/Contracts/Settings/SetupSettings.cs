namespace Spamma.App.Infrastructure.Contracts.Settings;

public class SetupSettings
{
    public DateTime? Completed { get; init; }

    public string Version { get; init; } = string.Empty;
}