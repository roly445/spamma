using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;

internal class CreateDomainCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<CreateDomainCommand>
{
    public Task<AuthorizationResult> Authorize(CreateDomainCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return Task.FromResult(AuthorizationResult.Fail());
        }

        return Task.FromResult(user.SystemRole.HasFlag(SystemRole.DomainManagement) ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}

