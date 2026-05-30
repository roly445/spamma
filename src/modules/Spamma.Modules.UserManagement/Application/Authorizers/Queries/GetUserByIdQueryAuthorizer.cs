using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetUserByIdQueryAuthorizer(IInternalQueryStore internalQueryStore, IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<GetUserByIdQuery>
{
    public Task<AuthorizationResult> Authorize(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (internalQueryStore.IsQueryStored(request))
        {
            return Task.FromResult(AuthorizationResult.Succeed());
        }

        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        return Task.FromResult(user.SystemRole.HasFlag(SystemRole.UserManagement) ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

