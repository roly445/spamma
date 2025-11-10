using MediatR.Behaviors.Authorization;
using Spamma.Modules.DomainManagement.Client.Application.Queries;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

/// <summary>
/// Authorizer for GetChaosAddressBySubdomainAndLocalPartQuery.
/// This is an internal query used by the email processing system.
/// </summary>
public class GetChaosAddressBySubdomainAndLocalPartQueryAuthorizer : AbstractRequestAuthorizer<GetChaosAddressBySubdomainAndLocalPartQuery>
{
    public override void BuildPolicy(GetChaosAddressBySubdomainAndLocalPartQuery request)
    {
        // No authorization requirements - this is an internal system query
        // used by SMTP message processing
    }
}
