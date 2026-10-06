using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands.CatchAllSender;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.CatchAllSender;

internal class AssignUserToCatchAllSenderCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<AssignUserToCatchAllSenderCommand>
{
    public Task<AuthorizationResult> Authorize(AssignUserToCatchAllSenderCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated && user.SystemRole.HasFlag(SystemRole.DomainManagement)
            ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}
