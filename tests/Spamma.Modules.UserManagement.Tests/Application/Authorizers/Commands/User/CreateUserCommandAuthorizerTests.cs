using FluentAssertions;
using Moq;
using Spamma.Modules.Common;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Application.Authorizers.Commands.User;
using Spamma.Modules.UserManagement.Client.Application.Commands.User;
using Spamma.Modules.UserManagement.Tests.Application.Authorizers;

namespace Spamma.Modules.UserManagement.Tests.Application.Authorizers.Commands.User;

public class CreateUserCommandAuthorizerTests
{
    [Fact]
    public async Task Authorize_WhenQueryIsStored_Succeeds()
    {
        var internalQueryStore = new Mock<IInternalQueryStore>();
        var command = new CreateUserCommand(Guid.NewGuid(), "Test User", "test@example.com", true, SystemRole.UserManagement);
        internalQueryStore.Setup(x => x.IsQueryStored(command)).Returns(true);
        var authorizer = new CreateUserCommandAuthorizer(internalQueryStore.Object, AuthorizerTestContext.CreateUnauthenticated());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsAdministrator_Succeeds()
    {
        var internalQueryStore = new Mock<IInternalQueryStore>();
        var command = new CreateUserCommand(Guid.NewGuid(), "Test User", "test@example.com", true, SystemRole.UserManagement);
        internalQueryStore.Setup(x => x.IsQueryStored(command)).Returns(false);
        var authorizer = new CreateUserCommandAuthorizer(internalQueryStore.Object, AuthorizerTestContext.CreateAuthenticated(SystemRole.UserManagement));

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenUserIsNotAdministrator_Fails()
    {
        var internalQueryStore = new Mock<IInternalQueryStore>();
        var command = new CreateUserCommand(Guid.NewGuid(), "Test User", "test@example.com", true, SystemRole.UserManagement);
        internalQueryStore.Setup(x => x.IsQueryStored(command)).Returns(false);
        var authorizer = new CreateUserCommandAuthorizer(internalQueryStore.Object, AuthorizerTestContext.CreateAuthenticated());

        var result = await authorizer.Authorize(command, CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
    }
}
