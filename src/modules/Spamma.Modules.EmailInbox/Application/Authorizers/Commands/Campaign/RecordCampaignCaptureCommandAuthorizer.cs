using BluQube.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

internal class RecordCampaignCaptureCommandAuthorizer : IBluQubeAuthorizer<RecordCampaignCaptureCommand>
{
    public Task<AuthorizationResult> Authorize(RecordCampaignCaptureCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}
