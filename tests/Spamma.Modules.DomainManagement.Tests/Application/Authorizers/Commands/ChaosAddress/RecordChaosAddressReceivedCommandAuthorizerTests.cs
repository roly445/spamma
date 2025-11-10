using FluentAssertions;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.ChaosAddress;

public class RecordChaosAddressReceivedCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsNoRequirements_InternalSystemCommand()
    {
        // Arrange
        var authorizer = new RecordChaosAddressReceivedCommandAuthorizer();
        var chaosAddressId = Guid.NewGuid();
        var command = new RecordChaosAddressReceivedCommand(chaosAddressId, DateTime.UtcNow);

        // Act
        authorizer.BuildPolicy(command);

        // Assert - Internal system command has no authorization requirements
        authorizer.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentSubdomainIds()
    {
        // Arrange
        var chaosAddressId = Guid.NewGuid();
        var authorizer1 = new RecordChaosAddressReceivedCommandAuthorizer();
        var authorizer2 = new RecordChaosAddressReceivedCommandAuthorizer();
        var command1 = new RecordChaosAddressReceivedCommand(chaosAddressId, DateTime.UtcNow);
        var command2 = new RecordChaosAddressReceivedCommand(chaosAddressId, DateTime.UtcNow);

        // Act
        authorizer1.BuildPolicy(command1);
        authorizer2.BuildPolicy(command2);

        // Assert - Both should have no requirements
        authorizer1.Requirements.Should().BeEmpty();
        authorizer2.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void BuildPolicy_CalledMultipleTimes_RemainsEmpty()
    {
        // Arrange
        var authorizer = new RecordChaosAddressReceivedCommandAuthorizer();
        var chaosAddressId = Guid.NewGuid();
        var command = new RecordChaosAddressReceivedCommand(chaosAddressId, DateTime.UtcNow);

        // Act
        authorizer.BuildPolicy(command);
        authorizer.BuildPolicy(command);

        // Assert - Should remain empty
        authorizer.Requirements.Should().BeEmpty();
    }
}
