using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Subdomain;

public class UpdateSubdomainDetailsCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new UpdateSubdomainDetailsCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new UpdateSubdomainDetailsCommand(subdomainId, "Updated Subdomain");

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
        var authorizer = new UpdateSubdomainDetailsCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new UpdateSubdomainDetailsCommand(subdomainId, "Updated Subdomain");

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
        var authorizer1 = new UpdateSubdomainDetailsCommandAuthorizer();
        var authorizer2 = new UpdateSubdomainDetailsCommandAuthorizer();
        var command1 = new UpdateSubdomainDetailsCommand(subdomainId1, "Subdomain 1");
        var command2 = new UpdateSubdomainDetailsCommand(subdomainId2, "Subdomain 2");

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
