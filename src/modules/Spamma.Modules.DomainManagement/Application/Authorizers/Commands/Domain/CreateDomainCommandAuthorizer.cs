using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;

internal class CreateDomainCommandAuthorizer : AbstractRequestAuthorizer<CreateDomainCommand>
{
    public override void BuildPolicy(CreateDomainCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeDomainAdministratorRequirement());
    }
}