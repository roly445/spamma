using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

public class GetUserByIdQueryAuthorizer(ITempObjectStore tempObjectStore) : AbstractRequestAuthorizer<GetUserByIdQuery>
{
    public override void BuildPolicy(GetUserByIdQuery request)
    {
        if (tempObjectStore.IsStoringReferenceForObject(request))
        {
            return;
        }

        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}