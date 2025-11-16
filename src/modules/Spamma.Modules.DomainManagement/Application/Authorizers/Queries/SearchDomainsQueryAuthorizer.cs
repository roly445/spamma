using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class SearchDomainsQueryAuthorizer : AbstractRequestAuthorizer<SearchDomainsQuery>
{
    public override void BuildPolicy(SearchDomainsQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToAtLeastOneDomainRequirement());
    }
}