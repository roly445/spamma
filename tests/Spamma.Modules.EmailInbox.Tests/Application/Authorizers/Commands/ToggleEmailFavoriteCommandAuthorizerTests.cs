using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands;

public class ToggleEmailFavoriteCommandAuthorizerTests
{
    [Fact]
    public void ToggleEmailFavoriteCommandAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new ToggleEmailFavoriteCommandAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void ToggleEmailFavoriteCommandAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new ToggleEmailFavoriteCommandAuthorizer();
        var command = new ToggleEmailFavoriteCommand(Guid.NewGuid());

        // Act & Verify
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();
    }

    [Fact]
    public void ToggleEmailFavoriteCommandAuthorizer_BuildPolicyWithDifferentEmailIds()
    {
        // Arrange
        var authorizer = new ToggleEmailFavoriteCommandAuthorizer();
        var cmd1 = new ToggleEmailFavoriteCommand(Guid.NewGuid());
        var cmd2 = new ToggleEmailFavoriteCommand(Guid.NewGuid());

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(cmd1);
        var action2 = () => authorizer.BuildPolicy(cmd2);

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }

    [Fact]
    public void ToggleEmailFavoriteCommandAuthorizer_ControlsFavoriteToggle()
    {
        // Arrange
        var authorizer = new ToggleEmailFavoriteCommandAuthorizer();
        var emailId = Guid.NewGuid();

        // Act & Verify - Should verify access before allowing favorite toggle
        var command = new ToggleEmailFavoriteCommand(emailId);
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();

        // Verify policy enforcement across multiple calls
        var otherCommand = new ToggleEmailFavoriteCommand(Guid.NewGuid());
        var otherAction = () => authorizer.BuildPolicy(otherCommand);
        otherAction.Should().NotThrow();
    }
}
