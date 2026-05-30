using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

internal class UnsuspendAccountCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<UnsuspendAccountCommand>
{
    public Task<AuthorizationResult> Authorize(UnsuspendAccountCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        return Task.FromResult(user.SystemRole.HasFlag(SystemRole.UserManagement) ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

