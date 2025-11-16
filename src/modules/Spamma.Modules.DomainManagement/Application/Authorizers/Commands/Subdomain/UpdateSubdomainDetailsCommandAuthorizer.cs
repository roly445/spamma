using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;

internal class UpdateSubdomainDetailsCommandAuthorizer : AbstractRequestAuthorizer<UpdateSubdomainDetailsCommand>
{
    public override void BuildPolicy(UpdateSubdomainDetailsCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToSubdomainRequirement
        {
            SubdomainId = request.SubdomainId,
        });
    }
}