using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;

namespace Spamma.Modules.Common.Application.AuthorizationRequirements;

public class MustBeAuthenticatedRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class MustBeAuthenticatedRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeAuthenticatedRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeAuthenticatedRequirement request, CancellationToken cancellationToken = default)
        {
            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();

            return Task.FromResult(user.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}