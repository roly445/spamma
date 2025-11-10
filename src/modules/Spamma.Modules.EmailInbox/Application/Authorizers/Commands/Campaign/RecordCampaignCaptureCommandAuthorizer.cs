using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

public class RecordCampaignCaptureCommandAuthorizer : AbstractRequestAuthorizer<RecordCampaignCaptureCommand>
{
    public override void BuildPolicy(RecordCampaignCaptureCommand request)
    {
    }
}