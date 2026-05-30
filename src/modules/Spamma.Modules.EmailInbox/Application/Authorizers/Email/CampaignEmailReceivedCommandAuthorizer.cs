using BluQube.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Email;

internal class CampaignEmailReceivedCommandAuthorizer : IBluQubeAuthorizer<CampaignEmailReceivedCommand>
{
    public Task<AuthorizationResult> Authorize(CampaignEmailReceivedCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}
