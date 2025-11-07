using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;

public class CreateChaosAddressCommandAuthorizer : AbstractRequestAuthorizer<CreateChaosAddressCommand>
{
    public override void BuildPolicy(CreateChaosAddressCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToSubdomainRequirement()
        {
            SubdomainId = request.SubdomainId,
        });
    }
}