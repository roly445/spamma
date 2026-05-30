using FluentAssertions;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetMyApiKeysQueryAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenCalled_Succeeds()
    {
        var authorizer = new GetMyApiKeysQueryAuthorizer();

        var result = await authorizer.Authorize(new GetMyApiKeysQuery(), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }
}
