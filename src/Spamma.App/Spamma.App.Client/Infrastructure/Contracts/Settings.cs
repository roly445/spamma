namespace Spamma.App.Client.Infrastructure.Contracts;

public class Settings
{
    public string MailServerHostname { get; init; } = string.Empty;

    public int MxPriority { get; init; }

    public bool CatchAllModeEnabled { get; init; }
}