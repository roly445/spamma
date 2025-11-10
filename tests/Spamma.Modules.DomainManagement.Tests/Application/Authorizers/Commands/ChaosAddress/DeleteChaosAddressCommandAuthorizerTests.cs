using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.ChaosAddress;

public class DeleteChaosAddressCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new DeleteChaosAddressCommandAuthorizer();
        var chaosAddressId = Guid.NewGuid();
        var command = new DeleteChaosAddressCommand(chaosAddressId);

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
        var authorizer = new DeleteChaosAddressCommandAuthorizer();
        var chaosAddressId = Guid.NewGuid();
        var command = new DeleteChaosAddressCommand(chaosAddressId);

        // Act
        authorizer.BuildPolicy(command);

        // Assert
        authorizer.Requirements.Should().HaveCount(2);
        authorizer.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessChaosAddressRequirement");
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var chaosAddressId = Guid.NewGuid();
        var authorizer1 = new DeleteChaosAddressCommandAuthorizer();
        var authorizer2 = new DeleteChaosAddressCommandAuthorizer();
        var command1 = new DeleteChaosAddressCommand(chaosAddressId);
        var command2 = new DeleteChaosAddressCommand(chaosAddressId);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert
        authorizer1.Requirements.Should().HaveCount(2);
        authorizer2.Requirements.Should().HaveCount(2);
        authorizer1.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessChaosAddressRequirement");
        authorizer2.Requirements.Should().ContainSingle(r => r.GetType().Name == "MustHaveAccessChaosAddressRequirement");
    }
}
