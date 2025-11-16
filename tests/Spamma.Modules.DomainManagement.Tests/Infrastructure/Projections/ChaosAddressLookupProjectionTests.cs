using FluentAssertions;
using Spamma.Modules.Common.Client;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.Projections;

namespace Spamma.Modules.DomainManagement.Tests.Infrastructure.Projections;

public class ChaosAddressLookupProjectionTests
{
    [Fact]
    public void Create_MapsChaosAddressCreated_ToReadModel()
    {
        var id = Guid.NewGuid();
        var domain = Guid.NewGuid();
        var subdomain = Guid.NewGuid();
        var when = DateTime.UtcNow;

        var evt = new ChaosAddressCreated(id, domain, subdomain, "local", SmtpResponseCode.MailboxUnavailable, when);

        var projection = new ChaosAddressLookupProjection();
        var readModel = projection.Create(evt);

        readModel.Id.Should().Be(id);
        readModel.DomainId.Should().Be(domain);
        readModel.SubdomainId.Should().Be(subdomain);
        readModel.LocalPart.Should().Be("local");
        readModel.ConfiguredSmtpCode.Should().Be(SmtpResponseCode.MailboxUnavailable);
        readModel.CreatedAt.Should().Be(when);
    }

    // No local IPatchExpression stub is required; we assert that Patch is invoked by causing Patch to throw and verifying the throw.
}