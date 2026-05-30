using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetUserPasskeysQueryAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var authorizer = new GetUserPasskeysQueryAuthorizer(AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));

        var result = await authorizer.Authorize(new GetUserPasskeysQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsNotAdministrator_Fails()
    {
        var authorizer = new GetUserPasskeysQueryAuthorizer(AuthorizerTestContext.CreateAuthenticated());

        var result = await authorizer.Authorize(new GetUserPasskeysQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
