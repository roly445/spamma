using FluentAssertions;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.Services.BackgroundJobs;

namespace Spamma.Modules.EmailInbox.Tests.Domain;

public class CampaignFailureTests
{
    [Fact]
    public void TemporaryFailureRetries_CountOneMessageAndDistinctAttempts()
    {
        var now = DateTimeOffset.UtcNow;
        var failureId = Guid.NewGuid();
        var firstAttempt = Guid.NewGuid();
        var secondAttempt = Guid.NewGuid();
        var campaign = Campaign.ObserveFailure(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "bounce", now);

        campaign.RecordDeliveryFailure(failureId, firstAttempt, 450, now).IsSuccess.Should().BeTrue();
        campaign.RecordDeliveryFailure(failureId, firstAttempt, 450, now).IsSuccess.Should().BeTrue();
        campaign.RecordDeliveryFailure(failureId, secondAttempt, 450, now.AddMinutes(1)).IsSuccess.Should().BeTrue();

        campaign.GetUncommittedEvents().OfType<CampaignDeliveryFailureRecorded>().Should().ContainSingle();
        campaign.GetUncommittedEvents().OfType<CampaignDeliveryFailureRetried>().Should().ContainSingle();

        var replayed = Campaign.Replay(null, new CampaignObservedViaFailure(campaign.Id, campaign.DomainId, campaign.SubdomainId, "bounce", now));
        replayed = Campaign.Replay(replayed, new CampaignDeliveryFailureRecorded(failureId, firstAttempt, 450, now));
        replayed.RecordDeliveryFailure(failureId, secondAttempt, 450, now.AddMinutes(1)).IsSuccess.Should().BeTrue();
        replayed.GetUncommittedEvents().OfType<CampaignDeliveryFailureRetried>().Should().ContainSingle();
    }

    [Fact]
    public void FirstAcceptedMessageAfterBounce_BecomesSample()
    {
        var now = DateTimeOffset.UtcNow;
        var campaign = Campaign.ObserveFailure(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "bounce", now);
        var messageId = Guid.NewGuid();

        campaign.RecordCapture(messageId, now.AddMinutes(1)).IsSuccess.Should().BeTrue();
        campaign.SampleMessageId.Should().Be(messageId);
        campaign.GetUncommittedEvents().OfType<CampaignFirstCaptured>().Should().ContainSingle();
        campaign.RecordCapture(messageId, now.AddMinutes(2)).IsSuccess.Should().BeTrue();
        campaign.GetUncommittedEvents().OfType<CampaignFirstCaptured>().Should().ContainSingle();
    }

    [Fact]
    public void FailureIdentity_ScopesContentRecipientAndFailureClass()
    {
        var chaosId = Guid.NewGuid();
        var original = Identity("Message-ID: <reused@test>\r\nSubject: same\r\n\r\nbody one", chaosId, "one@test", 450);
        Identity("Message-ID: <reused@test>\r\nSubject: same\r\n\r\nbody one", chaosId, "ONE@test", 450).Should().Be(original);
        Identity("Message-ID: <reused@test>\r\nSubject: same\r\n\r\nbody two", chaosId, "one@test", 450).Should().NotBe(original);
        Identity("Subject: same\r\n\r\nbody one", chaosId, "one@test", 450).Should().NotBe(original);
        Identity("Message-ID: <reused@test>\r\nSubject: same\r\n\r\nbody one", chaosId, "two@test", 450).Should().NotBe(original);
        Identity("Message-ID: <reused@test>\r\nSubject: same\r\n\r\nbody one", chaosId, "one@test", 550).Should().NotBe(original);
    }

    private static Guid Identity(string mime, Guid chaosId, string recipient, int smtpCode)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(mime));
        return CampaignFailureIdentity.FromMessage(stream, chaosId, recipient, smtpCode);
    }
}
