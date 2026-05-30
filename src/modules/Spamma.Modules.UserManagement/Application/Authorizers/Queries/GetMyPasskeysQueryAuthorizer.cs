using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class GetMyPasskeysQueryAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<GetMyPasskeysQuery>
{
    public Task<AuthorizationResult> Authorize(GetMyPasskeysQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

