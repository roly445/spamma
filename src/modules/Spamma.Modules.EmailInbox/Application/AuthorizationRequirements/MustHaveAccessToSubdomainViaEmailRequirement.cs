using JetBrains.Annotations;
using Marten;
using MediatR.Behaviors.Authorization;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.AuthorizationRequirements;

public class MustHaveAccessToSubdomainViaEmailRequirement : IAuthorizationRequirement
{
    public required Guid EmailId { get; init; }

    [UsedImplicitly]
    private sealed class MustHaveAccessToSubdomainRequirementHandler(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession)
        : IAuthorizationHandler<MustHaveAccessToSubdomainViaEmailRequirement>
    {
        public async Task<AuthorizationResult> Handle(MustHaveAccessToSubdomainViaEmailRequirement request, CancellationToken cancellationToken = default)
        {
            var email = await documentSession.LoadAsync<EmailLookup>(request.EmailId, cancellationToken);
            if (email == null)
            {
                return AuthorizationResult.Fail();
            }

            var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
            if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
                user.ModeratedDomains.Contains(email.DomainId) ||
                user.ModeratedSubdomains.Contains(email.SubdomainId) ||
                user.ViewableSubdomains.Contains(email.SubdomainId))
            {
                return AuthorizationResult.Succeed();
            }

            return AuthorizationResult.Fail();
        }
    }
}