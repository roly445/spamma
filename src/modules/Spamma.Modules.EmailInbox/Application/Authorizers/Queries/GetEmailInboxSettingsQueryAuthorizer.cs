using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.EmailInbox.Client.Application.Queries;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetEmailInboxSettingsQueryAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<GetEmailInboxSettingsQuery>
{
    public Task<AuthorizationResult> Authorize(GetEmailInboxSettingsQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}
