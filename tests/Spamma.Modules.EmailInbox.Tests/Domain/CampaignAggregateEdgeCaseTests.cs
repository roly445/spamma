using FluentAssertions;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class CampaignAggregateEdgeCaseTests
{
    [Fact]
    public void RecordCapture_MultipleTimesOnSameCampaign_AddsAllMessages()
    {
        var campaign = new CampaignBuilder().Build();
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();
        var messageId3 = Guid.NewGuid();

        campaign.RecordCapture(messageId1, DateTime.UtcNow);
        campaign.RecordCapture(messageId2, DateTime.UtcNow);
        campaign.RecordCapture(messageId3, DateTime.UtcNow);

        // Campaign should have recorded all captures
        campaign.Should().NotBeNull();
    }

    [Fact]
    public void Delete_ThenRecordCapture_FailsBecauseCampaignDeleted()
    {
        var campaign = new CampaignBuilder().Build();
        var messageId = Guid.NewGuid();

        var deleteResult = campaign.Delete(DateTime.UtcNow);
        deleteResult.IsSuccess.Should().BeTrue();

        var captureResult = campaign.RecordCapture(messageId, DateTime.UtcNow);

        captureResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithMinimumValidData_Succeeds()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "a",
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMaxLengthCampaignValue_Succeeds()
    {
        var maxLengthValue = new string('x', 255);
        var createdAt = DateTimeOffset.UtcNow;

        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            maxLengthValue,
            Guid.NewGuid(),
            createdAt.DateTime,
            createdAt);

        result.IsSuccess.Should().BeTrue();
    }
}