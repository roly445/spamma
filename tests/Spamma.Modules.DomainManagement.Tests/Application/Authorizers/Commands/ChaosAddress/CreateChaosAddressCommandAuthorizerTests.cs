using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Application.Authorizers.Commands.ChaosAddress;
using Spamma.Modules.DomainManagement.Client.Application.Commands.ChaosAddress;

namespace Spamma.Modules.DomainManagement.Tests.Application.Authorizers.Commands.ChaosAddress;

public class CreateChaosAddressCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_AddsAuthenticationRequirement()
    {
        // Arrange
        var authorizer = new CreateChaosAddressCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new CreateChaosAddressCommand(Guid.NewGuid(), Guid.NewGuid(), subdomainId, "test", SmtpResponseCode.MailboxUnavailable);

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
        var authorizer = new CreateChaosAddressCommandAuthorizer();
        var subdomainId = Guid.NewGuid();
        var command = new CreateChaosAddressCommand(Guid.NewGuid(), Guid.NewGuid(), subdomainId, "test", SmtpResponseCode.MailboxUnavailable);

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
        var authorizer1 = new CreateChaosAddressCommandAuthorizer();
        var authorizer2 = new CreateChaosAddressCommandAuthorizer();
        var command1 = new CreateChaosAddressCommand(Guid.NewGuid(), Guid.NewGuid(), subdomainId1, "test1", SmtpResponseCode.MailboxUnavailable);
        var command2 = new CreateChaosAddressCommand(Guid.NewGuid(), Guid.NewGuid(), subdomainId2, "test2", SmtpResponseCode.MailboxUnavailable);

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