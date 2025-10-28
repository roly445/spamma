using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;

public class AuthenticateWithPasskeyCommandAuthorizer : AbstractRequestAuthorizer<AuthenticateWithPasskeyCommand>
{
    public override void BuildPolicy(AuthenticateWithPasskeyCommand request)
    {
        this.UseRequirement(new MustNotBeAuthenticatedRequirement());
    }
}