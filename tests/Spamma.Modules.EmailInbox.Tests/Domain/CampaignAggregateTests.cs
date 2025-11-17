using FluentAssertions;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class CampaignAggregateTests
{
    [Fact]
    public void Create_WithValidData_CreatesCampaignAndRaisesEvent()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(campaignId, domainId, subdomainId, "test@example.com", messageId, createdAt.DateTime, createdAt);

        // Verify
        result.ShouldBeOk(campaign =>
        {
            campaign.Id.Should().Be(campaignId);
            campaign.ShouldHaveRaisedEvent<CampaignCreated>(e =>
            {
                e.CampaignId.Should().Be(campaignId);
                e.DomainId.Should().Be(domainId);
                e.SubdomainId.Should().Be(subdomainId);
                e.CampaignValue.Should().Be("test@example.com");
                e.MessageId.Should().Be(messageId);
                e.CreatedAt.Should().Be(createdAt.DateTime);
            });
        });
    }

    [Fact]
    public void Create_WithEmptyDomainId_ReturnsFailed()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            "test@example.com",
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be(EmailInboxErrorCodes.InvalidCampaignData);
            error.Message.Should().Contain("DomainId cannot be empty");
        });
    }

    [Fact]
    public void Create_WithEmptySubdomainId_ReturnsFailed()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            "test@example.com",
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be(EmailInboxErrorCodes.InvalidCampaignData);
            error.Message.Should().Contain("SubdomainId cannot be empty");
        });
    }

    [Fact]
    public void Create_WithNullCampaignValue_ReturnsFailed()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null!,
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be(EmailInboxErrorCodes.InvalidCampaignData);
            error.Message.Should().Contain("CampaignValue cannot be empty");
        });
    }

    [Fact]
    public void Create_WithEmptyCampaignValue_ReturnsFailed()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be(EmailInboxErrorCodes.InvalidCampaignData);
            error.Message.Should().Contain("CampaignValue cannot be empty");
        });
    }

    [Fact]
    public void Create_WithCampaignValueExceedingMaxLength_ReturnsFailed()
    {
        // Arrange
        var longCampaignValue = new string('a', 256);
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            longCampaignValue,
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be(EmailInboxErrorCodes.InvalidCampaignData);
            error.Message.Should().Contain("CampaignValue must not exceed 255 characters");
        });
    }

    [Fact]
    public void Create_WithEmptyMessageId_ReturnsFailed()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            Guid.Empty,
            createdAt.DateTime,
            createdAt);

        // Verify
        result.ShouldBeFailed(error =>
        {
            error.Code.Should().Be(EmailInboxErrorCodes.InvalidCampaignData);
            error.Message.Should().Contain("MessageId cannot be empty");
        });
    }

    [Fact]
    public void RecordCapture_WithValidMessageId_RecordsCaptureAndRaisesEvent()
    {
        // Arrange
        var campaign = new Builders.CampaignBuilder().Build();
        var newMessageId = Guid.NewGuid();
        var capturedAt = DateTimeOffset.UtcNow.AddSeconds(1);

        // Act
        var result = campaign.RecordCapture(newMessageId, capturedAt);

        // Verify
        result.IsSuccess.Should().BeTrue();
        campaign.ShouldHaveRaisedEvent<CampaignCaptured>(e =>
        {
            e.CapturedAt.Should().Be(capturedAt);
        });
    }

    [Fact]
    public void RecordCapture_WhenCampaignDeleted_ReturnsFailed()
    {
        // Arrange
        var campaign = new Builders.CampaignBuilder().Build();
        campaign.Delete(DateTimeOffset.UtcNow.DateTime);

        var newMessageId = Guid.NewGuid();

        // Act
        var result = campaign.RecordCapture(newMessageId, DateTimeOffset.UtcNow);

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordCapture_WithEmptyMessageId_ReturnsFailed()
    {
        // Arrange
        var campaign = new Builders.CampaignBuilder().Build();

        // Act
        var result = campaign.RecordCapture(Guid.Empty, DateTimeOffset.UtcNow);

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Delete_WithValidData_DeletesCampaignAndRaisesEvent()
    {
        // Arrange
        var campaign = new Builders.CampaignBuilder().Build();
        var deletedAt = DateTimeOffset.UtcNow.AddSeconds(1);

        // Act
        var result = campaign.Delete(deletedAt.DateTime);

        // Verify
        result.IsSuccess.Should().BeTrue();
        campaign.ShouldHaveRaisedEvent<CampaignDeleted>(e =>
        {
            e.DeletedAt.Should().Be(deletedAt.DateTime);
        });
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ReturnsFailed()
    {
        // Arrange
        var campaign = new Builders.CampaignBuilder().Build();
        campaign.Delete(DateTimeOffset.UtcNow.DateTime);

        // Act
        var result = campaign.Delete(DateTimeOffset.UtcNow.AddSeconds(1).DateTime);

        // Verify
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordCapture_MultipleCaptures_AddsAllMessageIds()
    {
        // Arrange
        var campaign = new Builders.CampaignBuilder().Build();
        var messageId2 = Guid.NewGuid();
        var messageId3 = Guid.NewGuid();

        // Act
        campaign.RecordCapture(messageId2, DateTimeOffset.UtcNow);
        campaign.RecordCapture(messageId3, DateTimeOffset.UtcNow.AddSeconds(1));

        // Verify
        campaign.ShouldHaveRaisedEventCount(3); // CampaignCreated + 2x CampaignCaptured
    }
}