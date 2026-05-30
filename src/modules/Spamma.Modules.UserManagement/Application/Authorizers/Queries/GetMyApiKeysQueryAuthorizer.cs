using BluQube.Authorization;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetMyApiKeysQueryAuthorizer : IBluQubeAuthorizer<GetMyApiKeysQuery>
{
    public Task<AuthorizationResult> Authorize(GetMyApiKeysQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}

