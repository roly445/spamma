using System;
using FluentAssertions;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;
using Spamma.Tests.Common.Verification;
using Xunit;

namespace Spamma.Modules.DomainManagement.Tests.Domain.ChaosAddressAggregate;

public class ChaosAddressEditTests
{
    [Fact]
    public void Edit_WhenTotalReceivedIsZero_ChangesDomainAndSubdomainAndRaisesEvent()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var agg = create.ShouldBeOk();

        // ensure no receives yet
        agg.TotalReceived.Should().Be(0);

        var newDomain = Guid.NewGuid();
        var newSubdomain = Guid.NewGuid();

        var result = agg.Edit(newDomain, newSubdomain, "local2", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted);

        result.ShouldBeOk();
        agg.DomainId.Should().Be(newDomain);
        agg.SubdomainId.Should().Be(newSubdomain);

        // Verify a ChaosAddressSubdomainChanged event exists in the event list
        agg.UncommittedEvents.Should().Contain(e => e.GetType().Name == "ChaosAddressSubdomainChanged");
    }

    [Fact]
    public void Edit_WhenTotalReceivedGreaterThanZero_DomainChangeFails()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var agg = create.ShouldBeOk();

        // simulate a receive to make it immutable for domain changes
        agg.RecordReceive(when.AddSeconds(1)).ShouldBeOk();
        agg.TotalReceived.Should().BeGreaterThan(0);

        var newDomain = Guid.NewGuid();
        var newSubdomain = Guid.NewGuid();

        var result = agg.Edit(newDomain, newSubdomain, "local2", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted);

        result.ShouldBeFailed();

        // Domain/Subdomain should remain unchanged
        agg.DomainId.Should().Be(domainId);
        agg.SubdomainId.Should().Be(subdomainId);

        // No ChaosAddressSubdomainChanged event should be present
        agg.UncommittedEvents.Should().NotContain(e => e.GetType().Name == "ChaosAddressSubdomainChanged");
    }

    [Fact]
    public void Edit_WhenNewSubdomainIsSameAsExisting_DoesNotRaiseSubdomainChanged()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var agg = create.ShouldBeOk();

        var result = agg.Edit(domainId, subdomainId, "local-updated", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable);

        result.ShouldBeOk();

        // local part changed but domain/subdomain same -> no subdomain changed event
        agg.UncommittedEvents.Should().NotContain(e => e.GetType().Name == "ChaosAddressSubdomainChanged");
    }
}
