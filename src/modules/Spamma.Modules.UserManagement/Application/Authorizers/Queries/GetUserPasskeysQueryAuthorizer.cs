using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetUserPasskeysQueryAuthorizer : AbstractRequestAuthorizer<GetUserPasskeysQuery>
{
    public override void BuildPolicy(GetUserPasskeysQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}