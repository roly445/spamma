using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;

public class ToggleEmailFavoriteCommandAuthorizer : AbstractRequestAuthorizer<ToggleEmailFavoriteCommand>
{
    public override void BuildPolicy(ToggleEmailFavoriteCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToSubdomainViaEmailRequirement
        {
            EmailId = request.EmailId,
        });
    }
}