using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

internal class RecordCampaignCaptureCommandAuthorizer : AbstractRequestAuthorizer<RecordCampaignCaptureCommand>
{
    public override void BuildPolicy(RecordCampaignCaptureCommand request)
    {
    }
}