using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Subdomain;

public class SuspendSubdomainCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new SuspendSubdomainCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new SuspendSubdomainCommand(subdomainId, SubdomainSuspensionReason.PolicyViolation, "Violation");

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
        var authorizer = new SuspendSubdomainCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new SuspendSubdomainCommand(subdomainId, SubdomainSuspensionReason.PolicyViolation, "Violation");

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
        var authorizer1 = new SuspendSubdomainCommandAuthorizer();
        var authorizer2 = new SuspendSubdomainCommandAuthorizer();
        var command1 = new SuspendSubdomainCommand(subdomainId1, SubdomainSuspensionReason.PolicyViolation, "Violation 1");
        var command2 = new SuspendSubdomainCommand(subdomainId2, SubdomainSuspensionReason.PolicyViolation, "Violation 2");

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