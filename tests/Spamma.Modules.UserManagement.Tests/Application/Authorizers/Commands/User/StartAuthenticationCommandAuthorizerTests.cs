using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class StartAuthenticationCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsUnauthenticated_Succeeds()
    {
        var authorizer = new StartAuthenticationCommandAuthorizer(AuthorizerTestContext.CreateUnauthenticated());
        var command = new StartAuthenticationCommand("test@example.com");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsAuthenticated_Fails()
    {
        var authorizer = new StartAuthenticationCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated());
        var command = new StartAuthenticationCommand("test@example.com");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
