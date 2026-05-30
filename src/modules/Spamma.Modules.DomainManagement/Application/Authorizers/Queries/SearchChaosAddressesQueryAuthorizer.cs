using BluQube.Authorization;
using Marten;
using Microsoft.AspNetCore.Http;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Client.Application.Queries;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;

namespace Spamma.Modules.DomainManagement.Application.Authorizers.Queries;

internal class SearchChaosAddressesQueryAuthorizer(IHttpContextAccessor httpContextAccessor, IDocumentSession documentSession) : IBluQubeAuthorizer<SearchChaosAddressesQuery>
{
    public async Task<AuthorizationResult> Authorize(SearchChaosAddressesQuery request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext.ToUserAuthInfo();
        if (!user.IsAuthenticated)
        {
            return AuthorizationResult.Fail();
        }

        if (request.SubdomainId.HasValue)
        {
            if (user.SystemRole.HasFlag(SystemRole.DomainManagement) ||
                user.ModeratedSubdomains.Contains(request.SubdomainId.Value) ||
                user.ViewableSubdomains.Contains(request.SubdomainId.Value))
            {
                return AuthorizationResult.Succeed();
            }

            var hasSubdomainAccess = await documentSession.Query<SubdomainLookup>()
                .AnyAsync(x => x.Id == request.SubdomainId.Value && user.ModeratedDomains.Contains(x.DomainId), token: cancellationToken);

            return hasSubdomainAccess ? AuthorizationResult.Succeed() : AuthorizationResult.Fail();
        }

        if (user.SystemRole.HasFlag(SystemRole.DomainManagement) || user.ModeratedDomains.Any() || user.ModeratedSubdomains.Any())
        {
            return AuthorizationResult.Succeed();
        }

        return AuthorizationResult.Fail();
    }
}

