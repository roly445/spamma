using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;

internal class DeleteCampaignCommandAuthorizer : AbstractRequestAuthorizer<DeleteCampaignCommand>
{
    public override void BuildPolicy(DeleteCampaignCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToCampaignRequirement
        {
            CampaignId = request.CampaignId,
        });
    }
}