using BluQube.Authorization;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.ApiKey;

internal class CreateApiKeyCommandAuthorizer : IBluQubeAuthorizer<CreateApiKeyCommand>
{
    public Task<AuthorizationResult> Authorize(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}

