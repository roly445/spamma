using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Subdomain;

public class CheckMxRecordCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new CheckMxRecordCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new CheckMxRecordCommand(subdomainId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustBeAuthenticatedRequirement");
    }

    [Fact]
    public void BuildPolicy_AddsAccessToSubdomainRequirement()
    {
        // Arrange
        var authorizer = new CheckMxRecordCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new CheckMxRecordCommand(subdomainId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var subdomainId1 = Guid.NewGuid();
        var subdomainId2 = Guid.NewGuid();
        var authorizer1 = new CheckMxRecordCommandAuthorizer();
        var authorizer2 = new CheckMxRecordCommandAuthorizer();
        var command1 = new CheckMxRecordCommand(subdomainId1);
        var command2 = new CheckMxRecordCommand(subdomainId2);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessToSubdomainRequirement");
    }
}