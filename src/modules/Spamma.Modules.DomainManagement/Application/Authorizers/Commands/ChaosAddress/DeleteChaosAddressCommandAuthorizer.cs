using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;

public class DeleteChaosAddressCommandAuthorizer : AbstractRequestAuthorizer<DeleteChaosAddressCommand>
{
    public override void BuildPolicy(DeleteChaosAddressCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessChaosAddressRequirement
        {
            ChaosAddressId = request.ChaosAddressId,
        });
    }
}