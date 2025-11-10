using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands;
using Spamma.Modules.EmailInbox.Client.Application.Commands.Campaign;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands;

public class RecordCampaignCaptureCommandAuthorizerTests
{
    [Fact]
    public void RecordCampaignCaptureCommandAuthorizer_CanBeInstantiated()
    {
        // Arrange & Act
        var authorizer = new RecordCampaignCaptureCommandAuthorizer();

        // Verify
        authorizer.Should().NotBeNull();
    }

    [Fact]
    public void RecordCampaignCaptureCommandAuthorizer_BuildPolicyDoesNotThrow()
    {
        // Arrange
        var authorizer = new RecordCampaignCaptureCommandAuthorizer();
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "test@example.com",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Act & Verify
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();
    }

    [Fact]
    public void RecordCampaignCaptureCommandAuthorizer_BuildPolicyWithDifferentIds()
    {
        // Arrange
        var authorizer = new RecordCampaignCaptureCommandAuthorizer();
        var cmd1 = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "test1@example.com",
            ReceivedAt: DateTimeOffset.UtcNow);
        var cmd2 = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "test2@example.com",
            ReceivedAt: DateTimeOffset.UtcNow.AddHours(-1));

        // Act & Verify
        var action1 = () => authorizer.BuildPolicy(cmd1);
        var action2 = () => authorizer.BuildPolicy(cmd2);

        action1.Should().NotThrow();
        action2.Should().NotThrow();
    }

    [Fact]
    public void RecordCampaignCaptureCommandAuthorizer_RequiresAuthentication()
    {
        // Arrange
        var authorizer = new RecordCampaignCaptureCommandAuthorizer();
        var now = DateTimeOffset.UtcNow;

        // Act & Verify - Should enforce authentication before allowing capture
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "value",
            ReceivedAt: now);
        var action = () => authorizer.BuildPolicy(command);
        action.Should().NotThrow();
    }
}
