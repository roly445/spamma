using FluentAssertions;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class LocalDomainDetectorTests
{
    [Theory]
    [InlineData("mail.spamma.dev.localhost", true)]
    [InlineData("example.local", true)]
    [InlineData("localhost", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("mail.spamma.io", false)]
    [InlineData("example.com", false)]
    public void IsLocalDomain_ReturnsExpectedResult(string domain, bool expected)
    {
        // Act
        var result = LocalDomainDetector.IsLocalDomain(domain);

        // Verify
        result.Should().Be(expected);
    }
}
