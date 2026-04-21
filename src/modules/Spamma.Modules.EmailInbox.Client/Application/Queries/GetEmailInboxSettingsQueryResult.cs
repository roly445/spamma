using BluQube.Queries;

namespace Spamma.Modules.EmailInbox.Client.Application.Queries;

public record GetEmailInboxSettingsQueryResult(bool CatchAllModeEnabled) : IQueryResult;
