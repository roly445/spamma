using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

public class CreateUserCommandAuthorizer(ITempObjectStore tempObjectStore) : AbstractRequestAuthorizer<CreateUserCommand>
{
    public override void BuildPolicy(CreateUserCommand request)
    {
        if (tempObjectStore.IsStoringReferenceForObject(request))
        {
            return;
        }

        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}