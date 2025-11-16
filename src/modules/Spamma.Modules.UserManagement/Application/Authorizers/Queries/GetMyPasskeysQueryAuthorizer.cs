using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetMyPasskeysQueryAuthorizer : AbstractRequestAuthorizer<GetMyPasskeysQuery>
{
    public override void BuildPolicy(GetMyPasskeysQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}