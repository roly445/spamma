using FluentAssertions;
using Marten;
using Moq;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Retention;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class EmailRetentionPolicyServiceTests
{
    [Fact]
    public async Task GetEffectiveAsync_Uses24HourDomainDefaultWhenNoPolicyExists()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        var effective = await new EmailRetentionPolicyService(session.Object)
            .GetEffectiveAsync(domainId, subdomainId, CancellationToken.None);

        effective.MaxAgeHours.Should().Be(24);
        effective.MaxMessages.Should().BeNull();
    }

    [Fact]
    public async Task GetEffectiveAsync_SubdomainInheritsBothDomainLimits()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var parent = new EmailRetentionPolicy(EmailRetentionPolicy.DomainKey(domainId), EmailRetentionMode.Override, 48, 10);
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<EmailRetentionPolicy>(parent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        var effective = await new EmailRetentionPolicyService(session.Object)
            .GetEffectiveAsync(domainId, subdomainId, CancellationToken.None);

        effective.Should().Be(parent);
    }

    [Fact]
    public async Task GetEffectiveAsync_SubdomainOverrideCanDisableEitherLimit()
    {
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();
        var child = new EmailRetentionPolicy(EmailRetentionPolicy.SubdomainKey(subdomainId), EmailRetentionMode.Override, null, 3);
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<EmailRetentionPolicy>(child.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(child);

        var effective = await new EmailRetentionPolicyService(session.Object)
            .GetEffectiveAsync(domainId, subdomainId, CancellationToken.None);

        effective.Should().Be(child);
        effective.MaxAgeHours.Should().BeNull();
        effective.MaxMessages.Should().Be(3);
        session.Verify(
            x => x.LoadAsync<EmailRetentionPolicy>(
                EmailRetentionPolicy.DomainKey(domainId), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
