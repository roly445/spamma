using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;
using Spamma.Tests.Common.Verification;

namespace Spamma.Modules.DomainManagement.Tests.Domain;

public class ChaosAddressAggregateTests
{
    [Fact]
    public void Create_Should_Set_Fields()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var result = ChaosAddress.Create(id, domainId, Guid.NewGuid(), "test", SmtpResponseCode.RequestedActionAborted, when);

        var agg = result.ShouldBeOk();
        agg.Id.Should().Be(id);
        agg.LocalPart.Should().Be("test");
        agg.ConfiguredSmtpCode.Should().Be(SmtpResponseCode.RequestedActionAborted);
        agg.Enabled.Should().BeFalse();
    }

    [Fact]
    public void RecordReceive_FirstTime_Should_Increment_And_MarkImmutable()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var result = ChaosAddress.Create(id, domainId, Guid.NewGuid(), "test", SmtpResponseCode.RequestedActionAborted, when);
        var agg = result.ShouldBeOk();

        agg.RecordReceive(when.AddSeconds(1)).ShouldBeOk();
        agg.TotalReceived.Should().Be(1);
        agg.LastReceivedAt.Should().NotBe(DateTimeOffset.MinValue);
    }

    [Fact]
    public void RecordReceive_ReplayedJob_DoesNotCountAnotherSmtpAttempt()
    {
        var when = DateTimeOffset.UtcNow;
        var attemptId = Guid.NewGuid();
        var aggregate = ChaosAddress.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "soft",
            SmtpResponseCode.MailboxUnavailable, when.UtcDateTime).ShouldBeOk();

        aggregate.RecordReceive(when, attemptId).ShouldBeOk();
        aggregate.RecordReceive(when.AddSeconds(1), attemptId).ShouldBeOk();

        aggregate.TotalReceived.Should().Be(1);
        aggregate.GetUncommittedEvents().OfType<Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events.ChaosAddressReceivedV2>()
            .Should().ContainSingle();
    }

    [Fact]
    public void Enable_Disable_Works()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var result = ChaosAddress.Create(id, domainId, Guid.NewGuid(), "test", SmtpResponseCode.RequestedActionAborted, when);
        var agg = result.ShouldBeOk();

        agg.Enable(when.AddMinutes(1)).ShouldBeOk();
        agg.Enabled.Should().BeTrue();

        agg.Disable(when.AddMinutes(2)).ShouldBeOk();
        agg.Enabled.Should().BeFalse();
    }
}
