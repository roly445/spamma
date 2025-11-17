using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetEmailByIdQueryAuthorizer : AbstractRequestAuthorizer<GetEmailByIdQuery>
{
    public override void BuildPolicy(GetEmailByIdQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToSubdomainViaEmailRequirement
        {
            EmailId = request.EmailId,
        });
    }
}