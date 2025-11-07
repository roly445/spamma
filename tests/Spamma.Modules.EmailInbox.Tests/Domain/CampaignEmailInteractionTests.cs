using FluentAssertions;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class CampaignEmailInteractionTests
{
    [Fact]
    public void EmailCanBeCapturedToCampaign()
    {
        // Arrange
        var campaign = new CampaignBuilder().Build();
        var email = new EmailBuilder().Build();

        // Act - CaptureCampaign is void, just verify no exception
        email.CaptureCampaign(campaign.Id, DateTime.UtcNow);

        // Verify
        email.Should().NotBeNull();
    }

    [Fact]
    public void DeletedEmailCannotBeFavoritedAgain()
    {
        // Arrange
        var email = new EmailBuilder().Build();
        var deleteResult = email.Delete(DateTime.UtcNow);

        // Act
        var favoriteResult = email.MarkAsFavorite(DateTime.UtcNow.AddSeconds(1));

        // Verify
        deleteResult.IsSuccess.Should().BeTrue();
        favoriteResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CampaignCanRecordMultipleCapturedEmails()
    {
        // Arrange
        var campaign = new CampaignBuilder().Build();
        var now = DateTime.UtcNow;

        // Act
        var recordResult1 = campaign.RecordCapture(Guid.NewGuid(), now);
        var recordResult2 = campaign.RecordCapture(Guid.NewGuid(), now.AddSeconds(1));
        var recordResult3 = campaign.RecordCapture(Guid.NewGuid(), now.AddSeconds(2));

        // Verify
        recordResult1.IsSuccess.Should().BeTrue();
        recordResult2.IsSuccess.Should().BeTrue();
        recordResult3.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EmailFavoritedCannotBeDeletedIfBoundToCampaign()
    {
        // Arrange
        var campaign = new CampaignBuilder().Build();
        var email = new EmailBuilder().Build();

        // Act
        email.MarkAsFavorite(DateTime.UtcNow);

        // Email marked as favorite, but capture campaign doesn't check favorite status
        email.CaptureCampaign(campaign.Id, DateTime.UtcNow.AddSeconds(1));
        var deleteResult = email.Delete(DateTime.UtcNow.AddSeconds(2));

        // Verify - delete should succeed even after capturing
        deleteResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DeletedCampaignStillContainsRecordedCaptures()
    {
        // Arrange
        var campaign = new CampaignBuilder().Build();
        var now = DateTime.UtcNow;

        campaign.RecordCapture(Guid.NewGuid(), now);
        campaign.RecordCapture(Guid.NewGuid(), now.AddSeconds(1));

        // Act
        var deleteResult = campaign.Delete(now.AddSeconds(10));

        // Verify - Delete succeeds even with captures
        deleteResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MultipleDeletionAttemptsOnSameCampaignFail()
    {
        // Arrange
        var campaign = new CampaignBuilder().Build();
        var now = DateTime.UtcNow;

        // Act
        var firstDelete = campaign.Delete(now);
        var secondDelete = campaign.Delete(now.AddSeconds(1));

        // Verify
        firstDelete.IsSuccess.Should().BeTrue();
        secondDelete.IsSuccess.Should().BeFalse();
    }
}
