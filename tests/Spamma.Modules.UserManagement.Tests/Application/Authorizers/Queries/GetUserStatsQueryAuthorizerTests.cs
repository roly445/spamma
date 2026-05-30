using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetUserStatsQueryAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAuthenticated_Succeeds()
    {
        var authorizer = new GetUserStatsQueryAuthorizer(AuthorizerTestContext.CreateAuthenticated());

        var result = await authorizer.Authorize(new GetUserStatsQuery(), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsUnauthenticated_Fails()
    {
        var authorizer = new GetUserStatsQueryAuthorizer(AuthorizerTestContext.CreateUnauthenticated());

        var result = await authorizer.Authorize(new GetUserStatsQuery(), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
