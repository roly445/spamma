using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands;

public class CreateSubdomainCommandAuthorizer : AbstractRequestAuthorizer<CreateSubdomainCommand>
{
    public override void BuildPolicy(CreateSubdomainCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToDomainRequirement
        {
            DomainId = request.DomainId,
        });
    }
}