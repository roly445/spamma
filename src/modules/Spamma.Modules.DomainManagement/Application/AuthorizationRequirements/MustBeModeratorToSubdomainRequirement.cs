using JetBrains.Annotations;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

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
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedSubdomains.Contains(request.SubdomainId))
            {
                return AuthorizationResult.Succeed();
            }

            var hasDomainAccess = await documentSession.Query<SubdomainLookup>()
                .AnyAsync(
                    x => x.Id == request.SubdomainId && user.ModeratedDomains.Contains(x.DomainId),
                    token: cancellationToken);

            return hasDomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
        }
    }
}