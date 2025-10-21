using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands;

public class StartAuthenticationCommandAuthorizer : AbstractRequestAuthorizer<StartAuthenticationCommand>
{
    public override void BuildPolicy(StartAuthenticationCommand request)
    {
        this.UseRequirement(new MustNotBeAuthenticatedRequirement());
    }
}