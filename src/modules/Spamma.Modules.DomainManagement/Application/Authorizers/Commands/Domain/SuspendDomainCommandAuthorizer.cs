using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands;

public class SuspendDomainCommandAuthorizer : AbstractRequestAuthorizer<SuspendDomainCommand>
{
    public override void BuildPolicy(SuspendDomainCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToDomainRequirement
        {
            DomainId = request.DomainId,
        });
    }
}