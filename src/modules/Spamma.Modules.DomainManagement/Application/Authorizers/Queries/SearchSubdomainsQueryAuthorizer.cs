using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

public class SearchSubdomainsQueryAuthorizer(IInternalQueryStore internalQueryStore) : AbstractRequestAuthorizer<SearchSubdomainsQuery>
{
    public override void BuildPolicy(SearchSubdomainsQuery request)
    {
        if (!internalQueryStore.IsQueryStored(request))
        {
            this.UseRequirement(new MustBeAuthenticatedRequirement());
            this.UseRequirement(new MustBeModeratorToAtLeastOneSubdomainRequirement());
        }
    }
}