using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class ChangeDetailsCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var authorizer = new ChangeDetailsCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));
        var command = new ChangeDetailsCommand(Guid.NewGuid(), "test@example.com", "Test User", SystemRole.UserManagement);

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsAuthenticatedButNotAdministrator_Fails()
    {
        var authorizer = new ChangeDetailsCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated());
        var command = new ChangeDetailsCommand(Guid.NewGuid(), "test@example.com", "Test User", SystemRole.UserManagement);

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
