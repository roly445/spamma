using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers;

public class GetPasskeyByCredentialIdQueryAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenCalled_Succeeds()
    {
        var authorizer = new GetPasskeyByCredentialIdQueryAuthorizer();

        var result = await authorizer.Authorize(new GetPasskeyByCredentialIdQuery(Array.Empty<byte>()), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }
}
