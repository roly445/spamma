using BluQube.Queries;

namespace Spamma.Modules.Common.Client.Application.Queries;

public record GetSystemSettingsQueryResult(bool CatchAllModeEnabled) : IQueryResult;
