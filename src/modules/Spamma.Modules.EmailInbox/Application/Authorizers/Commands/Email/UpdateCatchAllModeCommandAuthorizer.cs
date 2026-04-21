using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;

internal class UpdateCatchAllModeCommandAuthorizer : AbstractRequestAuthorizer<UpdateCatchAllModeCommand>
{
    public override void BuildPolicy(UpdateCatchAllModeCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
