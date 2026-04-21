using Spamma.Modules.EmailInbox.Infrastructure.Constants;

namespace Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

public class EmailInboxSettingsDocument
{
    public static readonly Guid SettingsId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public static readonly Guid CatchAllDomainId = CatchAllConstants.DomainId;

    public static readonly Guid CatchAllSubdomainId = CatchAllConstants.SubdomainId;

    public Guid Id { get; set; } = SettingsId;

    public bool CatchAllModeEnabled { get; set; }
}
