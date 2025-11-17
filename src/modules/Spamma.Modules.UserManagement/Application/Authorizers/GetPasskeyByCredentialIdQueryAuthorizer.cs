using MediatR.Behaviors.Authorization;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers;

internal class GetPasskeyByCredentialIdQueryAuthorizer : AbstractRequestAuthorizer<GetPasskeyByCredentialIdQuery>
{
    public override void BuildPolicy(GetPasskeyByCredentialIdQuery request)
    {
    }
}
