using FluentValidation;
using Marten;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.DomainManagement.Application.CommandHandlers.Dns;
using Spamma.Modules.DomainManagement.Client.Application.Commands.Dns;
using Spamma.Modules.DomainManagement.Infrastructure.Dns;

namespace Spamma.Modules.DomainManagement.Tests.Application.CommandHandlers.Dns;

public class CreateDnsIntegrationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ProtectsCredentialBeforeSaving()
    {
        var id = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        var protectionProvider = new EphemeralDataProtectionProvider();
        DnsIntegration? saved = null;
        session.Setup(x => x.LoadAsync<DnsIntegration>(id, CancellationToken.None)).ReturnsAsync((DnsIntegration?)null);
        session.Setup(x => x.Store(It.IsAny<DnsIntegration[]>())).Callback<DnsIntegration[]>(x => saved = x.Single());
        var handler = new CreateDnsIntegrationCommandHandler(session.Object, protectionProvider, TimeProvider.System,
            Array.Empty<IValidator<CreateDnsIntegrationCommand>>(), new Mock<ILogger<CreateDnsIntegrationCommandHandler>>().Object);

        await handler.Handle(new CreateDnsIntegrationCommand(id, " Main ", "Cloudflare", "Example.com", new string('a', 32), "test-token"), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("example.com", saved.ZoneName);
        Assert.NotEqual("test-token", saved.ProtectedCredential);
        Assert.Equal("test-token", protectionProvider.CreateProtector("Spamma.DomainManagement.DnsCredential.v1").Unprotect(saved.ProtectedCredential));
        session.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateId_DoesNotOverwriteIntegration()
    {
        var id = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<DnsIntegration>(id, CancellationToken.None)).ReturnsAsync(new DnsIntegration { Id = id });
        var handler = new CreateDnsIntegrationCommandHandler(session.Object, new EphemeralDataProtectionProvider(), TimeProvider.System,
            Array.Empty<IValidator<CreateDnsIntegrationCommand>>(), new Mock<ILogger<CreateDnsIntegrationCommandHandler>>().Object);

        await handler.Handle(new CreateDnsIntegrationCommand(id, "Main", "Cloudflare", "example.com", new string('a', 32), "test-token"), CancellationToken.None);

        session.Verify(x => x.Store(It.IsAny<DnsIntegration[]>()), Times.Never);
    }
}
