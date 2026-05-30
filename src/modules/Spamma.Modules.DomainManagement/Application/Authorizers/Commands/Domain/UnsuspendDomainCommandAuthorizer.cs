using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;

internal class UnsuspendDomainCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<UnsuspendDomainCommand>
{
    public Task<AuthorizationResult> Authorize(UnsuspendDomainCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        var isAuthorized = user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedDomains.Contains(request.DomainId);
        return Task.FromResult(isAuthorized ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

