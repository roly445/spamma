using FluentValidation;
using Marten;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Dns;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Dns;

public class UpdateDnsIntegrationCommandHandlerTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("new-token", true)]
    public async Task Handle_UpdatesDetailsAndOnlyReplacesCredentialWhenProvided(string? credential, bool rotatesCredential)
    {
        var id = Guid.NewGuid();
        var protectionProvider = new EphemeralDataProtectionProvider();
        var protector = protectionProvider.CreateProtector("Spamma.DomainManagement.DnsCredential.v1");
        var existing = new DnsIntegration
        {
            Id = id,
            Name = "Old",
            Provider = "Cloudflare",
            ZoneName = "old.example.com",
            ZoneId = new string('a', 32),
            ProtectedCredential = protector.Protect("old-token"),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        };
        var session = new Mock<IDocumentSession>();
        DnsIntegration? saved = null;
        session.Setup(x => x.LoadAsync<DnsIntegration>(id, CancellationToken.None)).ReturnsAsync(existing);
        session.Setup(x => x.Store(It.IsAny<DnsIntegration[]>())).Callback<DnsIntegration[]>(x => saved = x.Single());
        var handler = new UpdateDnsIntegrationCommandHandler(session.Object, protectionProvider,
            Array.Empty<IValidator<UpdateDnsIntegrationCommand>>(), new Mock<ILogger<UpdateDnsIntegrationCommandHandler>>().Object);

        await handler.Handle(new UpdateDnsIntegrationCommand(id, " New ", "Cloudflare", "Example.com", new string('b', 32), credential), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("New", saved.Name);
        Assert.Equal("example.com", saved.ZoneName);
        Assert.Equal(existing.CreatedAt, saved.CreatedAt);
        Assert.Equal(rotatesCredential ? "new-token" : "old-token", protector.Unprotect(saved.ProtectedCredential));
        session.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
}
