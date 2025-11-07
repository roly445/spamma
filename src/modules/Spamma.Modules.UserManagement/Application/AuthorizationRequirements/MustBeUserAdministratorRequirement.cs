using System.Security.Claims;
using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.UserManagement.Application.AuthorizationRequirements;

public class MustBeUserAdministratorRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeUserAdministratorRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeUserAdministratorRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeUserAdministratorRequirement request, CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;

            var claim = context?.User.FindFirst(ClaimTypes.Role)?.Value;
            if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles))
            {
                return userRoles.HasFlag(SystemRole.UserManagement)
                    ? Task.FromResult(AuthorizationResult.Succeed())
                    : Task.FromResult(AuthorizationResult.Fail());
            }

            return Task.FromResult(AuthorizationResult.Fail());
        }
    }
}