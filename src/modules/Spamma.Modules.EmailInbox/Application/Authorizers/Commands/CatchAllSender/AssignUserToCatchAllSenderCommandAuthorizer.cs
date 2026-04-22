using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.CatchAllSender;

internal class AssignUserToCatchAllSenderCommandAuthorizer : AbstractRequestAuthorizer<AssignUserToCatchAllSenderCommand>
{
    public override void BuildPolicy(AssignUserToCatchAllSenderCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
