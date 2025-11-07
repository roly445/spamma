using JetBrains.Annotations;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustHaveAccessChaosAddressRequirement : IAuthorizationRequirement
{
    public required Guid ChaosAddressId { get; init; }

    [UsedImplicitly]
    private sealed class MustHaveAccessChaosAddressRequirementHandler(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession)
        : IAuthorizationHandler<MustHaveAccessChaosAddressRequirement>
    {
        public async Task<AuthorizationResult> Handle(MustHaveAccessChaosAddressRequirement request, CancellationToken cancellationToken = default)
        {
            var chaosAddress = await documentSession.LoadAsync<ChaosAddressLookup>(request.ChaosAddressId, cancellationToken);
            if (chaosAddress == null)
            {
                return AuthorizationResult.Fail();
            }

            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedSubdomains.Contains(chaosAddress.SubdomainId) || user.ModeratedSubdomains.Contains(chaosAddress.SubdomainId) || user.ViewableSubdomains.Contains(chaosAddress.SubdomainId))
            {
                return AuthorizationResult.Succeed();
            }

            var hasSubdomainAccess = await documentSession.Query<SubdomainLookup>()
                .AnyAsync(
                    x => x.Id == chaosAddress.SubdomainId && user.ModeratedDomains.Contains(x.DomainId),
                    token: cancellationToken);

            return hasSubdomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
        }
    }
}