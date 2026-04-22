using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.CatchAllSender;

internal class UnassignUserFromCatchAllSenderCommandAuthorizer : AbstractRequestAuthorizer<UnassignUserFromCatchAllSenderCommand>
{
    public override void BuildPolicy(UnassignUserFromCatchAllSenderCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
