using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Domain;

public class UpdateDomainDetailsCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new UpdateDomainDetailsCommandAuthorizer();
        var domainId = Guid.NewGuid();
        var command = new UpdateDomainDetailsCommand(domainId, null, "Updated Domain");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsModeratorToDomainRequirement()
    {
        // Arrange
        var authorizer = new UpdateDomainDetailsCommandAuthorizer();
        var domainId = Guid.NewGuid();
        var command = new UpdateDomainDetailsCommand(domainId, null, "Updated Domain");

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToDomainRequirement");
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentDomainIds()
    {
        // Arrange
        var domainId1 = Guid.NewGuid();
        var domainId2 = Guid.NewGuid();
        var authorizer1 = new UpdateDomainDetailsCommandAuthorizer();
        var authorizer2 = new UpdateDomainDetailsCommandAuthorizer();
        var command1 = new UpdateDomainDetailsCommand(domainId1, null, "Domain 1");
        var command2 = new UpdateDomainDetailsCommand(domainId2, null, "Domain 2");

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToDomainRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeModeratorToDomainRequirement");
    }
}
