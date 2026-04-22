using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetCatchAllSenderAddressDetailQueryAuthorizer : AbstractRequestAuthorizer<GetCatchAllSenderAddressDetailQuery>
{
    public override void BuildPolicy(GetCatchAllSenderAddressDetailQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
