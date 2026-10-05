using System.Security.Claims;
using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.Common.Client.Infrastructure.Constants;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class SearchUsersQueryAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var authorizer = new SearchUsersQueryAuthorizer(AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));

        var result = await authorizer.Authorize(new SearchUsersQuery(), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsNotAdministrator_Fails()
    {
        var authorizer = new SearchUsersQueryAuthorizer(AuthorizerTestContext.CreateAuthenticated());

        var result = await authorizer.Authorize(new SearchUsersQuery(), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Authorize_WhenUserModeratesASubdomain_Succeeds()
    {
        var context = AuthorizerTestContext.CreateAuthenticated();
        ((ClaimsIdentity)context.HttpContext!.User.Identity!).AddClaim(
            new Claim(Lookups.ModeratedSubdomainClaim, Guid.NewGuid().ToString()));
        var authorizer = new SearchUsersQueryAuthorizer(context);

        var result = await authorizer.Authorize(new SearchUsersQuery(SearchTerm: "candidate"), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }
}
