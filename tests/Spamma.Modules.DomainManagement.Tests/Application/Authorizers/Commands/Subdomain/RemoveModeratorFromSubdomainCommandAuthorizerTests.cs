using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Subdomain;

public class RemoveModeratorFromSubdomainCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new RemoveModeratorFromSubdomainCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveModeratorFromSubdomainCommand(subdomainId, userId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsModeratorToSubdomainRequirement()
    {
        // Arrange
        var authorizer = new RemoveModeratorFromSubdomainCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveModeratorFromSubdomainCommand(subdomainId, userId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var authorizer1 = new RemoveModeratorFromSubdomainCommandAuthorizer();
        var authorizer2 = new RemoveModeratorFromSubdomainCommandAuthorizer();
        var command1 = new RemoveModeratorFromSubdomainCommand(subdomainId1, userId);
        var command2 = new RemoveModeratorFromSubdomainCommand(subdomainId2, userId);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToSubdomainRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToSubdomainRequirement");
    }
}
