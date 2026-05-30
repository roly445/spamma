using FluentAssertions;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Authorizers.Queries;
using Spamma.Modules.UserManagement.Client.Application.Queries;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Queries;

public class GetUserByIdQueryAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenQueryIsStored_Succeeds()
    {
        var internalQueryStore = new Mock<IInternalQueryStore>();
        var query = new GetUserByIdQuery(Guid.NewGuid());
        internalQueryStore.Setup(x => x.IsQueryStored(query)).Returns(true);
        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStore.Object, AuthorizerTestContext.CreateUnauthenticated());

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var internalQueryStore = new Mock<IInternalQueryStore>();
        var query = new GetUserByIdQuery(Guid.NewGuid());
        internalQueryStore.Setup(x => x.IsQueryStored(query)).Returns(false);
        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStore.Object, AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsNotAdministrator_Fails()
    {
        var internalQueryStore = new Mock<IInternalQueryStore>();
        var query = new GetUserByIdQuery(Guid.NewGuid());
        internalQueryStore.Setup(x => x.IsQueryStored(query)).Returns(false);
        var authorizer = new GetUserByIdQueryAuthorizer(internalQueryStore.Object, AuthorizerTestContext.CreateAuthenticated());

        var result = await authorizer.Authorize(query, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
