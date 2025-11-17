using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Domain;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.Domain;

public class SuspendDomainCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new SuspendDomainCommandAuthorizer();
        var domainId = Guid.NewGuid();
        var command = new SuspendDomainCommand(domainId, DomainSuspensionReason.PolicyViolation, "Violation");

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
        var authorizer = new SuspendDomainCommandAuthorizer();
        var domainId = Guid.NewGuid();
        var command = new SuspendDomainCommand(domainId, DomainSuspensionReason.PolicyViolation, "Violation");

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
        var authorizer1 = new SuspendDomainCommandAuthorizer();
        var authorizer2 = new SuspendDomainCommandAuthorizer();
        var command1 = new SuspendDomainCommand(domainId1, DomainSuspensionReason.PolicyViolation, "Violation 1");
        var command2 = new SuspendDomainCommand(domainId2, DomainSuspensionReason.PolicyViolation, "Violation 2");

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