using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.ApiKey;

public class RevokeApiKeyCommandAuthorizer : AbstractRequestAuthorizer<RevokeApiKeyCommand>
{
    public override void BuildPolicy(RevokeApiKeyCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}