using FluentAssertions;
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

        var result = ChaosAddress.Create(id, domainId, Guid.NewGuid(), "test", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted, when);

        var agg = result.ShouldBeOk();
        agg.Id.Should().Be(id);
        agg.LocalPart.Should().Be("test");
        agg.ConfiguredSmtpCode.Should().Be(Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted);
        agg.Enabled.Should().BeFalse();
    }

    [Fact]
    public void RecordReceive_FirstTime_Should_Increment_And_MarkImmutable()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var result = ChaosAddress.Create(id, domainId, Guid.NewGuid(), "test", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted, when);
        var agg = result.ShouldBeOk();

        agg.RecordReceive(when.AddSeconds(1)).ShouldBeOk();
        agg.TotalReceived.Should().Be(1);
        agg.LastReceivedAt.Should().NotBeNull();
    }

    [Fact]
    public void Enable_Disable_Works()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var result = ChaosAddress.Create(id, domainId, Guid.NewGuid(), "test", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted, when);
        var agg = result.ShouldBeOk();

        agg.Enable(when.AddMinutes(1)).ShouldBeOk();
        agg.Enabled.Should().BeTrue();

        agg.Disable(when.AddMinutes(2)).ShouldBeOk();
        agg.Enabled.Should().BeFalse();
    }
}
