using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;

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
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (user.SystemRole.HasFlag(SystemRole.DomainManagement))
            {
                return Task.FromResult(AuthorizationResult.Succeed());
            }

            var domainClaims = user.ModeratedDomains;
            return Task.FromResult(domainClaims.Contains(request.DomainId) ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}