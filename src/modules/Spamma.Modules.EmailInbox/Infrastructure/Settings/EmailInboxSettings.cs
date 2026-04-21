namespace Spamma.Modules.EmailInbox.Infrastructure.Settings;

public class EmailInboxSettings
{
    public int Port { get; init; } = 25;

    public int CatchAllPort { get; init; } = 1026;

    public bool CatchAllPortEnabled { get; init; } = false;
}
