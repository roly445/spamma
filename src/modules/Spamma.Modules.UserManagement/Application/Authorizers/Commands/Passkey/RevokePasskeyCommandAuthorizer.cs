using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;

internal class RevokePasskeyCommandAuthorizer : AbstractRequestAuthorizer<RevokePasskeyCommand>
{
    public override void BuildPolicy(RevokePasskeyCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}