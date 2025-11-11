using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

/// <summary>
/// Authorizer for GetEmailContentQuery.
/// </summary>
public class GetEmailContentQueryAuthorizer : AbstractRequestAuthorizer<GetEmailContentQuery>
{
    /// <inheritdoc />
    public override void BuildPolicy(GetEmailContentQuery request)
    {
        this.UseRequirement(new AllowPublicApiAccessRequirement());
    }
}