using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class SearchSubdomainViewersQueryAuthorizer : AbstractRequestAuthorizer<SearchSubdomainViewersQuery>
{
    public override void BuildPolicy(SearchSubdomainViewersQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToAtLeastOneSubdomainRequirement());
    }
}