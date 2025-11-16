using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetMyApiKeysQueryAuthorizer : AbstractRequestAuthorizer<GetMyApiKeysQuery>
{
    public override void BuildPolicy(GetMyApiKeysQuery request)
    {
        this.UseRequirement(new AllowPublicApiAccessRequirement());
    }
}