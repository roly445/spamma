using BluQube.Queries;
using Marten;
using Microsoft.Extensions.Options;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.UserManagement.Application.QueryProcessors.User;

public class GetUserByIdQueryProcessor(IDocumentSession session, IOptions<Settings> settings) : IQueryProcessor<GetUserByIdQuery, GetUserByIdQueryResult>
{
    private const SystemRole AdminRoles = SystemRole.DomainManagement | SystemRole.UserManagement;

    public async Task<QueryResult<GetUserByIdQueryResult>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await session.Query<UserLookup>().SingleOrDefaultAsync(x => x.Id == request.UserId, token: cancellationToken);

        return user is null ? QueryResult<GetUserByIdQueryResult>.Failed()
            : QueryResult<GetUserByIdQueryResult>.Succeeded(new GetUserByIdQueryResult(
                user.Id,
                user.EmailAddress,
                user.Name,
                user.IsSuspended,
                GetSystemRoleForUser(user.Id, settings.Value.PrimaryUserId, user.SystemRole),
                user.ModeratedDomains,
                user.ModeratedSubdomains,
                user.ViewableSubdomains));
    }

    private static SystemRole GetSystemRoleForUser(Guid userId, Guid primaryUserId, SystemRole userSystemRole)
    {
        return userId == primaryUserId ? AdminRoles : userSystemRole;
    }
}