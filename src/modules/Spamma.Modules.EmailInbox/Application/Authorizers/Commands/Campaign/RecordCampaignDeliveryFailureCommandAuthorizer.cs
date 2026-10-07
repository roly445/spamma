using BluQube.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

internal class RecordCampaignDeliveryFailureCommandAuthorizer : IBluQubeAuthorizer<RecordCampaignDeliveryFailureCommand>
{
    public Task<AuthorizationResult> Authorize(RecordCampaignDeliveryFailureCommand request, CancellationToken cancellationToken)
        => Task.FromResult(AuthorizationResult.Succeed());
}
