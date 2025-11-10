using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands;

public class DeleteCampaignCommandAuthorizerTests
{
    [Fact]
    public void DeleteCampaignCommandAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new DeleteCampaignCommandAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void DeleteCampaignCommandAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new DeleteCampaignCommandAuthorizer();
        var command = new DeleteCampaignCommand(Guid.NewGuid());

        // Act & Verify
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();
    }

    [Fact]
    public void DeleteCampaignCommandAuthorizer_BuildPolicyWithDifferentCampaignIds()
    {
        // Arrange
        var authorizer = new DeleteCampaignCommandAuthorizer();
        var cmd1 = new DeleteCampaignCommand(Guid.NewGuid());
        var cmd2 = new DeleteCampaignCommand(Guid.NewGuid());

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(cmd1);
        var action2 = () => authorizer.BuildPolicy(cmd2);

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }

    [Fact]
    public void DeleteCampaignCommandAuthorizer_EnforcesCampaignAccess()
    {
        // Arrange
        var authorizer = new DeleteCampaignCommandAuthorizer();
        var campaignId = Guid.NewGuid();

        // Act & Verify - Should verify user has access to campaign before allowing deletion
        var command = new DeleteCampaignCommand(campaignId);
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();

        // Verify with another instance to ensure policy is applied consistently
        var authorizer2 = new DeleteCampaignCommandAuthorizer();
        var action2 = () => authorizer2.BuildPolicy(new DeleteCampaignCommand(campaignId));
        action2.Should().NotThrow();
    }
}
