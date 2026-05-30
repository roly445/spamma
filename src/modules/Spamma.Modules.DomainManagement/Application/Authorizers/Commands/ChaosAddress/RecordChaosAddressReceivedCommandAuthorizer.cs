using BluQube.Authorization;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;

internal class RecordChaosAddressReceivedCommandAuthorizer : IBluQubeAuthorizer<RecordChaosAddressReceivedCommand>
{
    public Task<AuthorizationResult> Authorize(RecordChaosAddressReceivedCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(AuthorizationResult.Succeed());
    }
}

