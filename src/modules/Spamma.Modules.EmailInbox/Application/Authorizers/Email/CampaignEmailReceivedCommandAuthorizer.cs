using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Email;

internal class CampaignEmailReceivedCommandAuthorizer : AbstractRequestAuthorizer<CampaignEmailReceivedCommand>
{
    public override void BuildPolicy(CampaignEmailReceivedCommand request)
    {
        // No authorization requirements - this is an internal system command
        // triggered by SMTP message processing
    }
}