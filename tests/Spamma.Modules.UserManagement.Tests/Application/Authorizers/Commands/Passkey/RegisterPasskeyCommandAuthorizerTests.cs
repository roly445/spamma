using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.Passkey;
using Spamma.Modules.UserManagement.Client.Application.Commands.PassKey;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.Passkey;

public class RegisterPasskeyCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenUserIsAuthenticated_Succeeds()
    {
        var authorizer = new RegisterPasskeyCommandAuthorizer(AuthorizerTestContext.CreateAuthenticated());
        var command = new RegisterPasskeyCommand(Array.Empty<byte>(), Array.Empty<byte>(), 0, "Test", "ES256");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsUnauthenticated_Fails()
    {
        var authorizer = new RegisterPasskeyCommandAuthorizer(AuthorizerTestContext.CreateUnauthenticated());
        var command = new RegisterPasskeyCommand(Array.Empty<byte>(), Array.Empty<byte>(), 0, "Test", "ES256");

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
