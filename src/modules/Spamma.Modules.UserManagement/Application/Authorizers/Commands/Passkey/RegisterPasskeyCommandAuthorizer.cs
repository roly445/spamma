using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;

public class RegisterPasskeyCommandAuthorizer : AbstractRequestAuthorizer<RegisterPasskeyCommand>
{
    public override void BuildPolicy(RegisterPasskeyCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}