using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.EmailInbox.Client.Application.Queries;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Application.Authorizers.Queries;

internal class GetEmailMimeMessageByIdQueryAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<GetEmailMimeMessageByIdQuery>
{
    public async Task<AuthorizationResult> Authorize(GetEmailMimeMessageByIdQuery request, CancellationToken cancellationToken)
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

        if (await EmailAccessAuthorizer.CanAccessAsync(user, email, documentSession, cancellationToken))
        {
            return AuthorizationResult.Succeed();
        }

        return AuthorizationResult.Fail();
    }
}
