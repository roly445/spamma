using BluQube.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetEmailContentQueryAuthorizer : IBluQubeAuthorizer<GetEmailContentQuery>
{
    public Task<AuthorizationResult> Authorize(GetEmailContentQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}
