using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class SearchCatchAllSenderAddressesQueryAuthorizer : AbstractRequestAuthorizer<SearchCatchAllSenderAddressesQuery>
{
    public override void BuildPolicy(SearchCatchAllSenderAddressesQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
