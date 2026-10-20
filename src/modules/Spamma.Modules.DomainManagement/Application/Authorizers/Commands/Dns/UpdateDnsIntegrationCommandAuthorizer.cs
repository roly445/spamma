using BluQube.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Dns;

internal class UpdateDnsIntegrationCommandAuthorizer(IHttpContextAccessor accessor) : IBluQubeAuthorizer<UpdateDnsIntegrationCommand>
{
    public Task<AuthorizationResult> Authorize(UpdateDnsIntegrationCommand request, CancellationToken cancellationToken)
    {
        var user = accessor.HttpContext.ToUserAuthInfo();
        return Task.FromResult(user.IsAuthenticated && user.SystemRole.HasFlag(SystemRole.DomainManagement)
            ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
    }
}
