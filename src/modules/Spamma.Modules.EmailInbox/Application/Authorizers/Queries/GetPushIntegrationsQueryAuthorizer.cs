using MediatR.Behaviors.Authorization;
using Spamma.Modules.EmailInbox.Client.Application.Queries.PushIntegration;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

/// <summary>
/// Authorizer for GetPushIntegrationsQuery.
/// </summary>
public class GetPushIntegrationsQueryAuthorizer : AbstractRequestAuthorizer<GetPushIntegrationsQuery>
{
    /// <inheritdoc />
    public override void BuildPolicy(GetPushIntegrationsQuery request)
    {
        // Authorization bypassed for now
    }
}