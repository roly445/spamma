using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;

namespace Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;

internal class CompleteAuthenticationCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<CompleteAuthenticationCommand>
{
    public Task<AuthorizationResult> Authorize(CompleteAuthenticationCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated ? AuthorizationResult.Fail() : AuthorizationResult.Succeed());
    }
}

