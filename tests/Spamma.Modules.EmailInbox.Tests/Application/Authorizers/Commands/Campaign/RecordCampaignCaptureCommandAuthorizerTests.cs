using FluentAssertions;
using Spamma.Modules.EmailInbox.Application.Authorizers.Commands.Campaign;
using Spamma.Modules.EmailInbox.Client.Application.Commands;

namespace Spamma.Modules.EmailInbox.Tests.Application.Authorizers.Commands.Campaign;

/// <summary>
/// Tests for RecordCampaignCaptureCommandAuthorizer to verify authorization requirements.
/// </summary>
public class RecordCampaignCaptureCommandAuthorizerTests
{
    [Fact]
    public void BuildPolicy_DoesNotAddAnyRequirements()
    {
        // Arrange
        var command = new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "test@example.com",
            ReceivedAt: DateTimeOffset.UtcNow);
        var authorizer = new RecordCampaignCaptureCommandAuthorizer();

        // Act
        authorizer.BuildPolicy(command);

        // Assert - This command is publicly accessible (SMTP processing)
        authorizer.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void BuildPolicy_WorksWithDifferentParameters()
    {
        // Arrange
        var authorizer1 = new RecordCampaignCaptureCommandAuthorizer();
        var authorizer2 = new RecordCampaignCaptureCommandAuthorizer();

        // Act
        authorizer1.BuildPolicy(new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "user1@example.com",
            ReceivedAt: DateTimeOffset.UtcNow));

        authorizer2.BuildPolicy(new RecordCampaignCaptureCommand(
            DomainId: Guid.NewGuid(),
            SubdomainId: Guid.NewGuid(),
            MessageId: Guid.NewGuid(),
            CampaignValue: "user2@test.com",
            ReceivedAt: DateTimeOffset.UtcNow.AddDays(-1)));

        // Assert
        authorizer1.Requirements.Should().BeEmpty();
        authorizer2.Requirements.Should().BeEmpty();
    }
}

