using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;

internal class ToggleEmailFavoriteCommandAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<ToggleEmailFavoriteCommand>
{
    public async Task<AuthorizationResult> Authorize(ToggleEmailFavoriteCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        var email = await documentSession.LoadAsync<EmailLookup>(request.EmailId, cancellationToken);
        if (email == null)
        {
            return AuthorizationResult.Fail();
        }

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
