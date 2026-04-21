using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetCatchAllEmailsQueryAuthorizer : AbstractRequestAuthorizer<GetCatchAllEmailsQuery>
{
    public override void BuildPolicy(GetCatchAllEmailsQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
