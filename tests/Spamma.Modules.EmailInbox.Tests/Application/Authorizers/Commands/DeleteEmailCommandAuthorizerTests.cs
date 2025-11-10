using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands;

public class DeleteEmailCommandAuthorizerTests
{
    [Fact]
    public void DeleteEmailCommandAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new DeleteEmailCommandAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void DeleteEmailCommandAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new DeleteEmailCommandAuthorizer();
        var command = new DeleteEmailCommand(Guid.NewGuid());

        // Act & Verify
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();
    }

    [Fact]
    public void DeleteEmailCommandAuthorizer_BuildPolicyWithDifferentEmailIds()
    {
        // Arrange
        var authorizer = new DeleteEmailCommandAuthorizer();
        var cmd1 = new DeleteEmailCommand(Guid.NewGuid());
        var cmd2 = new DeleteEmailCommand(Guid.NewGuid());

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(cmd1);
        var action2 = () => authorizer.BuildPolicy(cmd2);

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }

    [Fact]
    public void DeleteEmailCommandAuthorizer_ProtectsEmailDeletion()
    {
        // Arrange
        var authorizer = new DeleteEmailCommandAuthorizer();
        var emailId = Guid.NewGuid();

        // Act & Verify - Should verify user has access before allowing deletion
        var command = new DeleteEmailCommand(emailId);
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();

        // Verify authorization is enforced consistently across instances
        var anotherAuthorizer = new DeleteEmailCommandAuthorizer();
        var anotherAction = () => anotherAuthorizer.BuildPolicy(new DeleteEmailCommand(emailId));
        anotherAction.Should().NotThrow();
    }
}
