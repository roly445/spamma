using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries.Passkey;

/// <summary>
/// Authorizer for retrieving detailed information about a passkey (admin only).
/// </summary>
public class GetPasskeyDetailsQueryAuthorizer : AbstractRequestAuthorizer<GetPasskeyDetailsQuery>
{
    /// <summary>
    /// Builds the authorization policy for the query.
    /// </summary>
    /// <param name="request">The query request.</param>
    public override void BuildPolicy(GetPasskeyDetailsQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}