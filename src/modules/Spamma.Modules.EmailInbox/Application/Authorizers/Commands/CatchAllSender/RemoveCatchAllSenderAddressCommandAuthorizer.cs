using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.CatchAllSender;

internal class RemoveCatchAllSenderAddressCommandAuthorizer : AbstractRequestAuthorizer<RemoveCatchAllSenderAddressCommand>
{
    public override void BuildPolicy(RemoveCatchAllSenderAddressCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
