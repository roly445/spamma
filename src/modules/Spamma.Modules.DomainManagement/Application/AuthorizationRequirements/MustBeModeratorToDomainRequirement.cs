using System.Security.Claims;
using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeModeratorToDomainRequirement : IAuthorizationRequirement
{
    public required Guid DomainId { get; init; }

    [UsedImplicitly]
    private sealed class MustBeModeratorToDomainRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeModeratorToDomainRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeModeratorToDomainRequirement request, CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;
            var claim = context!.User.FindFirst(ClaimTypes.Role)?.Value;
            if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement))
            {
                return Task.FromResult(AuthorizationResult.Succeed());
            }

            // Check for any assigned domain claim, e.g., "AssignedDomain"
            var domainClaims = context.User.FindAll(Lookups.ModeratedDomainClaim).Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            return Task.FromResult(domainClaims.Contains(request.DomainId) ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}