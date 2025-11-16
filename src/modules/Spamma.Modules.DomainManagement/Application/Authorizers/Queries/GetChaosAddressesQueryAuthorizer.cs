using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class GetChaosAddressesQueryAuthorizer : AbstractRequestAuthorizer<GetChaosAddressesQuery>
{
    public override void BuildPolicy(GetChaosAddressesQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        if (request.SubdomainId.HasValue)
        {
            this.UseRequirement(new MustHaveAccessToSubdomainRequirement()
            {
                SubdomainId = request.SubdomainId.Value,
            });
        }
        else
        {
            this.UseRequirement(new MustBeModeratorToAtLeastOneSubdomainRequirement());
        }
    }
}