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
            var context = httpContextAccessor.HttpContext;
            return Task.FromResult(
                context!.User.Identity!.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}