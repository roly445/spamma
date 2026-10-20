namespace Spamma.Modules.EmailInbox.Infrastructure.Settings;

public class EmailInboxSettings
{
    public int Port { get; init; } = 25;

    public int ImplicitTlsPort { get; init; } = 465;

    public int StartTlsPort { get; init; } = 587;
}
