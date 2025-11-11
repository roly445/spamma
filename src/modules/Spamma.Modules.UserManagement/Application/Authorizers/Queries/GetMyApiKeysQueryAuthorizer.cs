using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries.ApiKeys;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

public class GetMyApiKeysQueryAuthorizer : AbstractRequestAuthorizer<GetMyApiKeysQuery>
{
    public override void BuildPolicy(GetMyApiKeysQuery request)
    {
        this.UseRequirement(new AllowPublicApiAccessRequirement());
    }
}