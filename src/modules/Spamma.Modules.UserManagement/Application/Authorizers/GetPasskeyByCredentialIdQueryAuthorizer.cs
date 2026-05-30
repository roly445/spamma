using BluQube.Authorization;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers;

internal class GetPasskeyByCredentialIdQueryAuthorizer : IBluQubeAuthorizer<GetPasskeyByCredentialIdQuery>
{
    public Task<AuthorizationResult> Authorize(GetPasskeyByCredentialIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}

