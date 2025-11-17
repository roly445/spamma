using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

internal class CompleteAuthenticationCommandAuthorizer : AbstractRequestAuthorizer<CompleteAuthenticationCommand>
{
    public override void BuildPolicy(CompleteAuthenticationCommand request)
    {
        this.UseRequirement(new MustNotBeAuthenticatedRequirement());
    }
}