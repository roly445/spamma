using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;

internal class SetupDomainDnsCommandAuthorizer(IHttpContextAccessor accessor) : IBluQubeAuthorizer<SetupDomainDnsCommand>
{
    public Task<AuthorizationResult> Authorize(SetupDomainDnsCommand request, CancellationToken cancellationToken)
    {
        var user = accessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated &&
            (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedDomains.Contains(request.DomainId))
            ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}
