namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class EmailInboxSettingsDocument
{
    public static readonly Guid SettingsId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static readonly Guid CatchAllDomainId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static readonly Guid CatchAllSubdomainId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; } = SettingsId;

    public bool CatchAllModeEnabled { get; set; }
}
