using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class UnsuspendAccountCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var authorizer = new UnsuspendAccountCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));
        var command = new UnsuspendAccountCommand(Guid.NewGuid());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsNotAdministrator_Fails()
    {
        var authorizer = new UnsuspendAccountCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated());
        var command = new UnsuspendAccountCommand(Guid.NewGuid());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
