using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.Passkey;

public class RevokeUserPasskeyCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var authorizer = new RevokeUserPasskeyCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));
        var command = new RevokeUserPasskeyCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsAuthenticatedButNotAdministrator_Fails()
    {
        var authorizer = new RevokeUserPasskeyCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated());
        var command = new RevokeUserPasskeyCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Authorize_WhenUserIsUnauthenticated_Fails()
    {
        var authorizer = new RevokeUserPasskeyCommandAuthorizer(AuthorizerTestContext.CreateUnauthenticated());
        var command = new RevokeUserPasskeyCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
