using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetEmailMimeMessageByIdQueryAuthorizer : AbstractRequestAuthorizer<GetEmailMimeMessageByIdQuery>
{
    public override void BuildPolicy(GetEmailMimeMessageByIdQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToSubdomainViaEmailRequirement
        {
            EmailId = request.EmailId,
        });
    }
}