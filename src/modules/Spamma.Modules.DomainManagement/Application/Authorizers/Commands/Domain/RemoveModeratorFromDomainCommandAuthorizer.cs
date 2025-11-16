using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;

internal class RemoveModeratorFromDomainCommandAuthorizer : AbstractRequestAuthorizer<RemoveModeratorFromDomainCommand>
{
    public override void BuildPolicy(RemoveModeratorFromDomainCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeModeratorToDomainRequirement
        {
            DomainId = request.DomainId,
        });
    }
}