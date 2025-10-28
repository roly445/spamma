using System.Security.Claims;
using JetBrains.Annotations;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustBeModeratorToSubdomainRequirement : IAuthorizationRequirement
{
    public required Guid SubdomainId { get; init; }

    [UsedImplicitly]
    private sealed class MustBeModeratorToSubdomainRequirementHandler(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession)
        : IAuthorizationHandler<MustBeModeratorToSubdomainRequirement>
    {
        public async Task<AuthorizationResult> Handle(MustBeModeratorToSubdomainRequirement request, CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;
            var claim = context?.User.FindFirst(ClaimTypes.Role)?.Value;
            if (claim != null && Enum.TryParse<SystemRole>(claim, out var userRoles) && userRoles.HasFlag(SystemRole.DomainManagement))
            {
                return AuthorizationResult.Succeed();
            }

            // Check for any assigned domain claim, e.g., "AssignedDomain"
            var subdomainClaims = context?.User?.FindAll(Lookups.ModeratedSubdomainClaim).Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (subdomainClaims != null && subdomainClaims.Contains(request.SubdomainId))
            {
                return AuthorizationResult.Succeed();
            }

            var domainClaims = context?.User?.FindAll(Lookups.ModeratedDomainClaim).Select(x =>
            {
                if (Guid.TryParse(x.Value, out var d))
                {
                    return d;
                }

                return (Guid?)null;
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList() ?? new List<Guid>();

            var hasDomainAccess = await documentSession.Query<SubdomainLookup>()
                .AnyAsync(
                    x => x.Id == request.SubdomainId && domainClaims.Contains(x.DomainId),
                    token: cancellationToken);

            return hasDomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
        }
    }
}