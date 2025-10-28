using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

public class UnsuspendAccountCommandAuthorizer : AbstractRequestAuthorizer<UnsuspendAccountCommand>
{
    public override void BuildPolicy(UnsuspendAccountCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}