using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

public class GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer : AbstractRequestAuthorizer<GetChaosAddressBySubdomainAndLocalPartQuery>
{
    public override void BuildPolicy(GetChaosAddressBySubdomainAndLocalPartQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToSubdomainRequirement
        {
            SubdomainId = request.SubdomainId,
        });
    }
}
