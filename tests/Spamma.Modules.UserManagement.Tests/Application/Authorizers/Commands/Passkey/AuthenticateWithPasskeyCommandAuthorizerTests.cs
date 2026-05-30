using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.Passkey;

public class AuthenticateWithPasskeyCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsUnauthenticated_Succeeds()
    {
        var authorizer = new AuthenticateWithPasskeyCommandAuthorizer(AuthorizerTestContext.CreateUnauthenticated());
        var command = new AuthenticateWithPasskeyCommand(Array.Empty<byte>(), 0, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), string.Empty, string.Empty, string.Empty);

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsAuthenticated_Fails()
    {
        var authorizer = new AuthenticateWithPasskeyCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated());
        var command = new AuthenticateWithPasskeyCommand(Array.Empty<byte>(), 0, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), string.Empty, string.Empty, string.Empty);

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
