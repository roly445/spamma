using Marten;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Tests.Infrastructure.Dns;

public class DnsSetupServiceTests
{
    private readonly IDataProtectionProvider protectionProvider = new EphemeralDataProtectionProvider();

    [Fact]
    public async Task EnsureAsync_ManualDomain_ReturnsManualFallback()
    {
        var service = new DnsSetupService(new Mock<IDocumentSession>().Object, this.protectionProvider, []);

        var result = await service.EnsureAsync(null, "example.com", new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task EnsureAsync_RecordOutsideZone_DoesNotCallProvider()
    {
        var provider = new FakeProvider();
        var integrationId = Guid.NewGuid();
        var session = this.CreateSession(integrationId, "example.com");
        var service = new DnsSetupService(session, this.protectionProvider, [provider]);

        var result = await service.EnsureAsync(integrationId, "example.com", new(DnsRecordType.Mx, "attacker.example.net", "mail.example.com", 3600, 10), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Failed, result.Status);
        Assert.False(provider.Called);
    }

    [Fact]
    public async Task EnsureAsync_SelectedIntegrationForAnotherZone_DoesNotCallProvider()
    {
        var provider = new FakeProvider();
        var integrationId = Guid.NewGuid();
        var session = this.CreateSession(integrationId, "other.example");
        var service = new DnsSetupService(session, this.protectionProvider, [provider]);

        var result = await service.EnsureAsync(integrationId, "example.com", new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.NotConfigured, result.Status);
        Assert.False(provider.Called);
    }

    [Fact]
    public async Task EnsureAsync_DecryptsSelectedCredentialForProvider()
    {
        var provider = new FakeProvider();
        var integrationId = Guid.NewGuid();
        var session = this.CreateSession(integrationId, "example.com");
        var service = new DnsSetupService(session, this.protectionProvider, [provider]);

        var result = await service.EnsureAsync(integrationId, "example.com", new(DnsRecordType.Txt, "example.com", "spamma-verification=abc", 3600), CancellationToken.None);

        Assert.Equal(DnsSetupStatus.Created, result.Status);
        Assert.Equal("test-token", provider.ReceivedCredential);
    }

    private IDocumentSession CreateSession(Guid integrationId, string zoneName)
    {
        var protectedCredential = this.protectionProvider.CreateProtector("Spamma.DomainManagement.DnsCredential.v1").Protect("test-token");
        var integration = new DnsIntegration
        {
            Id = integrationId,
            Provider = "Fake",
            ZoneName = zoneName,
            ZoneId = "zone-id",
            ProtectedCredential = protectedCredential,
        };
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<DnsIntegration>(integrationId, CancellationToken.None)).ReturnsAsync(integration);
        return session.Object;
    }

    private sealed class FakeProvider : IDnsRecordProvider
    {
        public string Name => "Fake";

        public bool Called { get; private set; }

        public string? ReceivedCredential { get; private set; }

        public Task<DnsSetupResult> EnsureAsync(DnsConnection connection, DnsRecordSpec record, CancellationToken cancellationToken)
        {
            this.Called = true;
            this.ReceivedCredential = connection.Credential;
            return Task.FromResult(new DnsSetupResult(DnsSetupStatus.Created, "Created"));
        }
    }
}
