using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeDomainAdministratorRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeDomainAdministratorRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeDomainAdministratorRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeDomainAdministratorRequirement request, CancellationToken cancellationToken = default)
        {
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (!user.IsAuthenticated)
            {
                return Task.FromResult(AuthorizationResult.Fail());
            }

            return user.SystemRole.HasFlag(SystemRole.DomainManagement)
                ? Task.FromResult(AuthorizationResult.Succeed())
                : Task.FromResult(AuthorizationResult.Fail());
        }
    }
}