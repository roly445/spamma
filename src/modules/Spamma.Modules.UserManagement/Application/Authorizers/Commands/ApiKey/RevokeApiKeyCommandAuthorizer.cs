using BluQube.Authorization;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.ApiKey;

internal class RevokeApiKeyCommandAuthorizer : IBluQubeAuthorizer<RevokeApiKeyCommand>
{
    public Task<AuthorizationResult> Authorize(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}

