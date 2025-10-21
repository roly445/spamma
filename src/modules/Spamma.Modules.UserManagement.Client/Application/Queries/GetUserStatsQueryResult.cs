using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

public record GetUserStatsQueryResult(int TotalUsers, int ActiveUsers, int SuspendedUsers) : IQueryResult;