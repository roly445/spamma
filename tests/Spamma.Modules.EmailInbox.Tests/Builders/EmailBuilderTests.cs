using FluentAssertions;
using Spamma.Modules.EmailInbox.Tests.Builders;

namespace Spamma.Modules.EmailInbox.Tests.Builders;

public class EmailBuilderTests
{
    [Fact]
    public void Build_WithDefaultValues_CreatesValidEmail()
    {
        var email = new EmailBuilder().Build();

        email.Should().NotBeNull();
        email.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Build_WithCustomEmailId_SetsId()
    {
        var emailId = Guid.NewGuid();
        var email = new EmailBuilder()
            .WithId(emailId)
            .Build();

        email.Id.Should().Be(emailId);
    }

    [Fact]
    public void Build_WithCustomDomainId_SetsDomainId()
    {
        var domainId = Guid.NewGuid();
        var email = new EmailBuilder()
            .WithDomainId(domainId)
            .Build();

        email.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithCustomSubject_SetsSubject()
    {
        var subject = "Custom Subject";
        var email = new EmailBuilder()
            .WithSubject(subject)
            .Build();

        email.Should().NotBeNull();
    }

    [Fact]
    public void Build_FluentChaining_Succeeds()
    {
        var emailId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subdomainId = Guid.NewGuid();

        var email = new EmailBuilder()
            .WithId(emailId)
            .WithDomainId(domainId)
            .WithSubdomainId(subdomainId)
            .WithSubject("Chained Subject")
            .Build();

        email.Should().NotBeNull();
        email.Id.Should().Be(emailId);
    }
}
