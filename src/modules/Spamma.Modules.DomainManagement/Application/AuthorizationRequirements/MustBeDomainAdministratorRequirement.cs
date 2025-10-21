using System.Security.Claims;
using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeDomainAdministratorRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeDomainAdministratorRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeDomainAdministratorRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeDomainAdministratorRequirement request, CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;

            var claim = context?.User.FindFirst(ClaimTypes.Role)?.Value;
            if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles))
            {
                return userRoles.HasFlag(SystemRole.DomainManagement)
                    ? Task.FromResult(AuthorizationResult.Succeed())
                    : Task.FromResult(AuthorizationResult.Fail());
            }

            return Task.FromResult(AuthorizationResult.Fail());
        }
    }
}