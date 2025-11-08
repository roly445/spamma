using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Subdomain;

public class CreateSubdomainCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new CreateSubdomainCommandAuthorizer();
        var domainId = Guid.NewGuid();
        var command = new CreateSubdomainCommand(Guid.NewGuid(), domainId, "mail.example.com", null);

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
        var authorizer = new CreateSubdomainCommandAuthorizer();
        var domainId = Guid.NewGuid();
        var command = new CreateSubdomainCommand(Guid.NewGuid(), domainId, "mail.example.com", null);

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
        var authorizer1 = new CreateSubdomainCommandAuthorizer();
        var authorizer2 = new CreateSubdomainCommandAuthorizer();
        var command1 = new CreateSubdomainCommand(Guid.NewGuid(), domainId1, "mail1.example.com", null);
        var command2 = new CreateSubdomainCommand(Guid.NewGuid(), domainId2, "mail2.example.com", null);

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
