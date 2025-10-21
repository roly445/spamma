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
            var context = httpContextAccessor.HttpContext;
            return Task.FromResult(
                context!.User.Identity!.IsAuthenticated ? AuthorizationResult.Fail() : AuthorizationResult.Succeed());
        }
    }
}