using MediatR.Behaviors.Authorization;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;

public class RecordChaosAddressReceivedCommandAuthorizer : AbstractRequestAuthorizer<RecordChaosAddressReceivedCommand>
{
    public override void BuildPolicy(RecordChaosAddressReceivedCommand request)
    {
    }
}