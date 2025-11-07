using MediatR.Behaviors.Authorization;
using Spamma.Modules.Common.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Application.AuthorizationRequirements;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries.Passkey;

/// <summary>
/// Authorizer for retrieving a specific user's passkeys (admin only).
/// </summary>
public class GetUserPasskeysQueryAuthorizer : AbstractRequestAuthorizer<GetUserPasskeysQuery>
{
    /// <summary>
    /// Builds the authorization policy for the query.
    /// </summary>
    /// <param name="request">The query request.</param>
    public override void BuildPolicy(GetUserPasskeysQuery request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
        this.UseRequirement(new MustBeUserAdministratorRequirement());
    }
}