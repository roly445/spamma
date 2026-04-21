using FluentAssertions;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SpammaMessageStoreCatchAllTests
{
    [Fact]
    public void CatchAllConstants_DomainId_IsWellKnownSentinel()
    {
        CatchAllConstants.DomainId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void CatchAllConstants_SubdomainId_IsWellKnownSentinel()
    {
        CatchAllConstants.SubdomainId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void CatchAllConstants_SentinelGuid_IsNotEmpty()
    {
        CatchAllConstants.DomainId.Should().NotBe(Guid.Empty);
        CatchAllConstants.SubdomainId.Should().NotBe(Guid.Empty);
    }
}
