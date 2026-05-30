using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;

internal class AuthenticateWithPasskeyCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<AuthenticateWithPasskeyCommand>
{
    public Task<AuthorizationResult> Authorize(AuthenticateWithPasskeyCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated ? AuthorizationResult.Fail() : AuthorizationResult.Succeed());
    }
}

