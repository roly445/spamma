using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands;

public class UnsuspendDomainCommandAuthorizer : AbstractRequestAuthorizer<UnsuspendDomainCommand>
{
    public override void BuildPolicy(UnsuspendDomainCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToDomainRequirement
        {
            DomainId = request.DomainId,
        });
    }
}