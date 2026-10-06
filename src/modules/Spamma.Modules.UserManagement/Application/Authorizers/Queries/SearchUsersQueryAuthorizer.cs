using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Queries;

internal class SearchUsersQueryAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<SearchUsersQuery>
{
    public Task<AuthorizationResult> Authorize(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        var canSearch = user.SystemRole.HasFlag(SystemRole.UserManagement) ||
                        user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
                        user.ModeratedDomains.Any() || user.ModeratedSubdomains.Any();
        return Task.FromResult(canSearch ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

