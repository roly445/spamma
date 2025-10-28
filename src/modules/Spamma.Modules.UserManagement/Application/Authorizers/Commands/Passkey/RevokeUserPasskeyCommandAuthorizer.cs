using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;

public class RevokeUserPasskeyCommandAuthorizer : AbstractRequestAuthorizer<RevokeUserPasskeyCommand>
{
    public override void BuildPolicy(RevokeUserPasskeyCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}