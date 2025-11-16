using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

internal class CreateUserCommandAuthorizer(IInternalQueryStore internalQueryStore) : AbstractRequestAuthorizer<CreateUserCommand>
{
    public override void BuildPolicy(CreateUserCommand request)
    {
        if (internalQueryStore.IsQueryStored(request))
        {
            return;
        }

        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}