using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetEmailInboxSettingsQueryAuthorizer : AbstractRequestAuthorizer<GetEmailInboxSettingsQuery>
{
    public override void BuildPolicy(GetEmailInboxSettingsQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
