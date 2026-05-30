using BluQube.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Email;

internal class ReceivedEmailCommandAuthorizer : IBluQubeAuthorizer<ReceivedEmailCommand>
{
    public Task<AuthorizationResult> Authorize(ReceivedEmailCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}
