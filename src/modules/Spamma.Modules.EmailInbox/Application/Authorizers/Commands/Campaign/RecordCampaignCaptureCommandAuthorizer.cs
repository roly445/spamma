using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

public class RecordCampaignCaptureCommandAuthorizer : AbstractRequestAuthorizer<RecordCampaignCaptureCommand>
{
    public override void BuildPolicy(RecordCampaignCaptureCommand request)
    {
    }
}