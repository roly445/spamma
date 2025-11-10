using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;

public class CheckMxRecordCommandAuthorizer : AbstractRequestAuthorizer<CheckMxRecordCommand>
{
    public override void BuildPolicy(CheckMxRecordCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustHaveAccessToSubdomainRequirement
        {
            SubdomainId = request.SubdomainId,
        });
    }
}