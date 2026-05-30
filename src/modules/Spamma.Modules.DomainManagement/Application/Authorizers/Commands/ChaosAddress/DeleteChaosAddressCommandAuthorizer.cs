using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;

internal class DeleteChaosAddressCommandAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<DeleteChaosAddressCommand>
{
    public async Task<AuthorizationResult> Authorize(DeleteChaosAddressCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        var chaosAddress = await documentSession.LoadAsync<ChaosAddressLookup>(request.ChaosAddressId, cancellationToken);
        if (chaosAddress == null)
        {
            return AuthorizationResult.Fail();
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
            user.ModeratedSubdomains.Contains(chaosAddress.SubdomainId) ||
            user.ViewableSubdomains.Contains(chaosAddress.SubdomainId))
        {
            return AuthorizationResult.Succeed();
        }

        var hasSubdomainAccess = await documentSession.Query<SubdomainLookup>()
            .AnyAsync(x => x.Id == chaosAddress.SubdomainId && user.ModeratedDomains.Contains(x.DomainId), token: cancellationToken);

        return hasSubdomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
    }
}

