using BluQube.Queries;
using Marten;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.User;

public class GetUserStatsQueryProcessor(IDocumentSession session)
    : IQueryProcessor<GetUserStatsQuery, GetUserStatsQueryResult>
{
    public async Task<QueryResult<GetUserStatsQueryResult>> Handle(
        GetUserStatsQuery request,
        CancellationToken cancellationToken)
    {
        var users = await session.Query<UserLookup>().Select(x => new
        {
            x.Id,
            x.IsSuspended,
            x.LastLoginAt,
        }).ToListAsync(token: cancellationToken);

        return QueryResult<GetUserStatsQueryResult>.Succeeded(new GetUserStatsQueryResult(
            users.Count,
            users.Count(x => !x.IsSuspended && x.LastLoginAt != null),
            users.Count(x => x.IsSuspended)));
    }
}