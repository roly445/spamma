using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Marten;
using Moq;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Submission;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SmtpSubmissionPolicyServiceTests
{
    [Fact]
    public async Task GetEffectiveAsync_InheritsDomainAuthenticationWhenSubdomainHasNoOverride()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var domain = Policy(SmtpSubmissionPolicy.DomainKey(domainId));
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(SmtpSubmissionPolicy.SubdomainKey(subdomainId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmtpSubmissionPolicy?)null);
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(SmtpSubmissionPolicy.DomainKey(domainId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(domain);

        var effective = await new SmtpSubmissionPolicyService(session.Object)
            .GetEffectiveAsync(domainId, subdomainId, CancellationToken.None);

        effective.Should().Be(domain);
        SmtpSubmissionPolicyService.Allows(effective, domain.Username).Should().BeTrue();
        SmtpSubmissionPolicyService.Allows(effective, null).Should().BeFalse();
    }

    [Theory]
    [InlineData(SmtpSubmissionMode.Disabled)]
    [InlineData(SmtpSubmissionMode.RequireAuthentication)]
    public async Task GetEffectiveAsync_SubdomainOverrideReplacesParentCredential(SmtpSubmissionMode mode)
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var parent = Policy(SmtpSubmissionPolicy.DomainKey(domainId));
        var child = mode == SmtpSubmissionMode.Disabled
            ? new SmtpSubmissionPolicy(SmtpSubmissionPolicy.SubdomainKey(subdomainId), mode, Guid.Empty, null)
            : Policy(SmtpSubmissionPolicy.SubdomainKey(subdomainId));
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(child.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(child);

        var effective = await new SmtpSubmissionPolicyService(session.Object)
            .GetEffectiveAsync(domainId, subdomainId, CancellationToken.None);

        effective.Should().Be(child);
        SmtpSubmissionPolicyService.Allows(effective, parent.Username).Should().Be(mode == SmtpSubmissionMode.Disabled);
        if (mode == SmtpSubmissionMode.RequireAuthentication)
        {
            SmtpSubmissionPolicyService.Allows(effective, child.Username).Should().BeTrue();
        }

        session.Verify(x => x.LoadAsync<SmtpSubmissionPolicy>(parent.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_RejectsRotatedCredentialAndWrongSecret()
    {
        var id = SmtpSubmissionPolicy.SubdomainKey(Guid.NewGuid());
        var current = Policy(id, "new-secret");
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<SmtpSubmissionPolicy>(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        var service = new SmtpSubmissionPolicyService(session.Object);

        (await service.AuthenticateAsync(current.Username!, "new-secret", CancellationToken.None)).Should().BeTrue();
        (await service.AuthenticateAsync(current.Username!, "wrong-secret", CancellationToken.None)).Should().BeFalse();
        (await service.AuthenticateAsync($"{id}:{Guid.NewGuid():N}", "new-secret", CancellationToken.None)).Should().BeFalse();
        (await service.AuthenticateAsync("invalid-user", "new-secret", CancellationToken.None)).Should().BeFalse();
    }

    private static SmtpSubmissionPolicy Policy(string id, string secret = "test-secret") => new(
        id, SmtpSubmissionMode.RequireAuthentication, Guid.NewGuid(),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))));
}
