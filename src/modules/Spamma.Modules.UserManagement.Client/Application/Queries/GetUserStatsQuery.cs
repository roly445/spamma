using BluQube.Attributes;
using BluQube.Queries;

namespace Spamma.Modules.UserManagement.Client.Application.Queries;

[BluQubeQuery(Path = "api/users/stats")]
public record GetUserStatsQuery : IQuery<GetUserStatsQueryResult>;