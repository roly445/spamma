using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

public class CompleteAuthenticationCommandAuthorizer : AbstractRequestAuthorizer<CompleteAuthenticationCommand>
{
    public override void BuildPolicy(CompleteAuthenticationCommand request)
    {
        this.UseRequirement(new MustNotBeAuthenticatedRequirement());
    }
}