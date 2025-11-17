using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetUserStatsQueryAuthorizer : AbstractRequestAuthorizer<GetUserStatsQuery>
{
    public override void BuildPolicy(GetUserStatsQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}