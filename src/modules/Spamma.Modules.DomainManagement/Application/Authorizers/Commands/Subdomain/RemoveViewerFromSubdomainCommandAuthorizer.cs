using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands;

public class RemoveViewerFromSubdomainCommandAuthorizer : AbstractRequestAuthorizer<RemoveViewerFromSubdomainCommand>
{
    public override void BuildPolicy(RemoveViewerFromSubdomainCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToSubdomainRequirement
        {
            SubdomainId = request.SubdomainId,
        });
    }
}