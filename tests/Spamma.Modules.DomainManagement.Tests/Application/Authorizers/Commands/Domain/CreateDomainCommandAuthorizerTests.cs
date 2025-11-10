using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Domain;

public class CreateDomainCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new CreateDomainCommandAuthorizer();
        var command = new CreateDomainCommand(Guid.NewGuid(), "example.com", null, null);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsDomainAdministratorRequirement()
    {
        // Arrange
        var authorizer = new CreateDomainCommandAuthorizer();
        var command = new CreateDomainCommand(Guid.NewGuid(), "example.com", null, null);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeDomainAdministratorRequirement");
    }

    [Fact]
    public void BuildPolicy_RequirementsAreIndependentOfDomainName()
    {
        // Arrange
        var authorizer1 = new CreateDomainCommandAuthorizer();
        var authorizer2 = new CreateDomainCommandAuthorizer();
        var command1 = new CreateDomainCommand(Guid.NewGuid(), "example.com", null, null);
        var command2 = new CreateDomainCommand(Guid.NewGuid(), "test.org", null, null);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Select(r => r.GetType().Name).Should().BeEquivalentTo(authorizer2.Requirements.Select(r => r.GetType().Name));
    }
}
