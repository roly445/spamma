using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;

namespace Spamma.Modules.Common.Application.AuthorizationRequirements;

public class MustNotBeAuthenticatedRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustNotBeAuthenticatedRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustNotBeAuthenticatedRequirement>
    {
        public Task<AuthorizationResult> Handle(MustNotBeAuthenticatedRequirement request, CancellationToken cancellationToken = default)
        {
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            return Task.FromResult(
                user.IsAuthenticated ? AuthorizationResult.Fail() : AuthorizationResult.Succeed());
        }
    }
}