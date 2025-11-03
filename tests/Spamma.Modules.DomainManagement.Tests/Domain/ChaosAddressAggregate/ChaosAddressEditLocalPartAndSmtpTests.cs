using System;
using FluentAssertions;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;
using Spamma.Tests.Common.Verification;
using Xunit;

namespace Spamma.Modules.DomainManagement.Tests.Domain.ChaosAddressAggregate;

public class ChaosAddressEditLocalPartAndSmtpTests
{
    [Fact]
    public void Edit_ChangeLocalPart_RaisesLocalPartChanged()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var agg = create.ShouldBeOk();

        var result = agg.Edit(domainId, subdomainId, "newlocal", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable);

        result.ShouldBeOk();
        agg.LocalPart.Should().Be("newlocal");
        agg.UncommittedEvents.Should().Contain(e => e.GetType().Name == "ChaosAddressLocalPartChanged");
    }

    [Fact]
    public void Edit_ChangeSmtpCode_RaisesSmtpCodeChanged()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var agg = create.ShouldBeOk();

        var result = agg.Edit(domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted);

        result.ShouldBeOk();
        agg.ConfiguredSmtpCode.Should().Be(Spamma.Modules.Common.SmtpResponseCode.RequestedActionAborted);
        agg.UncommittedEvents.Should().Contain(e => e.GetType().Name == "ChaosAddressSmtpCodeChanged");
    }

    [Fact]
    public void Edit_NoChanges_ReturnsNoEvents()
    {
        var id = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var create = ChaosAddress.Create(id, domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable, when);
        var agg = create.ShouldBeOk();

        var result = agg.Edit(domainId, subdomainId, "local", Spamma.Modules.Common.SmtpResponseCode.MailboxUnavailable);

        result.ShouldBeOk();
        // Expect no events because nothing changed
        agg.UncommittedEvents.Should().BeEmpty();
    }
}
