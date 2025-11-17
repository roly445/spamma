using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Email;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Email;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands.Email;

public class DeleteEmailCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var command = new DeleteEmailCommand(Guid.NewGuid());
        var authorizer = new DeleteEmailCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsSubdomainAccessViaEmailRequirement()
    {
        // Arrange
        var command = new DeleteEmailCommand(Guid.NewGuid());
        var authorizer = new DeleteEmailCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        var requirements = authorizer.Requirements;
        requirements.Should().HaveCount(2);
        requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainViaEmailRequirement");
    }

    [Fact]
    public void BuildPolicy_RequiresExactlyTwoRequirements()
    {
        // Arrange
        var command = new DeleteEmailCommand(Guid.NewGuid());
        var authorizer = new DeleteEmailCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentEmailIds()
    {
        // Arrange
        var emailId1 = Guid.NewGuid();
        var emailId2 = Guid.NewGuid();
        var authorizer1 = new DeleteEmailCommandAuthorizer();
        var authorizer2 = new DeleteEmailCommandAuthorizer();

        // Act
        authorizer1.BuildPolicy(new DeleteEmailCommand(emailId1));
        authorizer2.BuildPolicy(new DeleteEmailCommand(emailId2));

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
    }
}