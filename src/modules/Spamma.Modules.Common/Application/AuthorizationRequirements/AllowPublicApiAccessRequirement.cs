using JetBrains.Annotations;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;

namespace Spamma.Modules.Common.Application.AuthorizationRequirements;

public class AllowPublicApiAccessRequirement : IAuthorizationRequirement
{
    [UsedImplicitly]
    private sealed class AllowPublicApiAccessRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<AllowPublicApiAccessRequirement>
    {
        public Task<AuthorizationResult> Handle(AllowPublicApiAccessRequirement request, CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;
            return Task.FromResult(
                context!.User.Identity!.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}