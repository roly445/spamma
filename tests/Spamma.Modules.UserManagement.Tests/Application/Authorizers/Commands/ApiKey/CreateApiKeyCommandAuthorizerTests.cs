using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.ApiKey;
using Spamma.Modules.UserManagement.Client.Application.Commands.ApiKeys;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.ApiKey;

public class CreateApiKeyCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenCalled_Succeeds()
    {
        var authorizer = new CreateApiKeyCommandAuthorizer();
        var command = new CreateApiKeyCommand("My API Key", DateTime.UtcNow.AddDays(30));

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }
}
