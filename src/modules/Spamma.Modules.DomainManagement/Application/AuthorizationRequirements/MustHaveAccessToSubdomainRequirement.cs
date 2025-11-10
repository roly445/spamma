using JetBrains.Annotations;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.AuthorizationRequirements;

public class MustHaveAccessToSubdomainRequirement : IAuthorizationRequirement
{
    public required Guid SubdomainId { get; init; }

    [UsedImplicitly]
    private sealed class MustHaveAccessToSubdomainRequirementHandler(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession)
        : IAuthorizationHandler<MustHaveAccessToSubdomainRequirement>
    {
        public async Task<AuthorizationResult> Handle(MustHaveAccessToSubdomainRequirement request, CancellationToken cancellationToken = default)
        {
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedSubdomains.Contains(request.SubdomainId) || user.ModeratedSubdomains.Contains(request.SubdomainId) || user.ViewableSubdomains.Contains(request.SubdomainId))
            {
                return AuthorizationResult.Succeed();
            }

            var hasSubdomainAccess = await documentSession.Query<SubdomainLookup>()
                .AnyAsync(
                    x => x.Id == request.SubdomainId && user.ModeratedDomains.Contains(x.DomainId),
                    token: cancellationToken);

            return hasSubdomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
        }
    }
}