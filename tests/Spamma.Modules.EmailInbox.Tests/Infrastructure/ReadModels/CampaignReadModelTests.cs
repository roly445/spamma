using FluentAssertions;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.ReadModels;

public class CampaignReadModelTests
{
    [Fact]
    public void CampaignSample_Creation_WithValidData()
    {
        // Arrange & Act
        var campaignSample = new CampaignSample
        {
            CampaignId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Subject = "Test Subject",
            From = "sender@example.com",
            To = "recipient@example.com",
            ReceivedAt = DateTime.UtcNow,
            StoredAt = DateTime.UtcNow,
            ContentPreview = "This is a preview",
        };

        // Verify
        campaignSample.Should().NotBeNull();
        campaignSample.CampaignId.Should().NotBe(Guid.Empty);
        campaignSample.Subject.Should().Be("Test Subject");
        campaignSample.From.Should().Be("sender@example.com");
    }

    [Fact]
    public void CampaignSample_WithLongContentPreview()
    {
        // Arrange & Act
        var longPreview = new string('X', 1000);
        var campaignSample = new CampaignSample
        {
            CampaignId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Subject = "Subject with long preview",
            From = "from@example.com",
            To = "to@example.com",
            ReceivedAt = DateTime.UtcNow,
            StoredAt = DateTime.UtcNow,
            ContentPreview = longPreview,
        };

        // Verify
        campaignSample.ContentPreview.Should().HaveLength(1000);
    }

    [Fact]
    public void CampaignSample_StoredLaterThanReceived()
    {
        // Arrange & Act
        var receivedAt = DateTime.UtcNow;
        var storedAt = receivedAt.AddSeconds(5);

        var campaignSample = new CampaignSample
        {
            CampaignId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Subject = "Test",
            From = "from@example.com",
            To = "to@example.com",
            ReceivedAt = receivedAt,
            StoredAt = storedAt,
            ContentPreview = "Preview",
        };

        // Verify
        campaignSample.StoredAt.Should().BeAfter(campaignSample.ReceivedAt);
    }

    [Fact]
    public void CampaignSample_WithMultipleRecipients()
    {
        // Arrange & Act
        var campaignSample = new CampaignSample
        {
            CampaignId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Subject = "Multi-recipient email",
            From = "from@example.com",
            To = "to1@example.com; to2@example.com; to3@example.com",
            ReceivedAt = DateTime.UtcNow,
            StoredAt = DateTime.UtcNow.AddSeconds(1),
            ContentPreview = "Email with multiple recipients",
        };

        // Verify
        campaignSample.To.Should().Contain(";");
        campaignSample.To.Should().Contain("to2@example.com");
    }
}