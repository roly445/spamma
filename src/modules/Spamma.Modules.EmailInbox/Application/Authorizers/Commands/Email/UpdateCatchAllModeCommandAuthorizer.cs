using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;

internal class UpdateCatchAllModeCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<UpdateCatchAllModeCommand>
{
    public Task<AuthorizationResult> Authorize(UpdateCatchAllModeCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}
