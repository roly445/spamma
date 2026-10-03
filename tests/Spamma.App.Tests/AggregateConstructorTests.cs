using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate;
using Spamma.Modules.DomainManagement.Domain.DomainAggregate;
using Spamma.Modules.DomainManagement.Domain.SubdomainAggregate;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;
using Spamma.Modules.EmailInbox.Domain.CatchAllSenderAddressAggregate;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;
using Spamma.Modules.UserManagement.Domain.ApiKeys;
using Spamma.Modules.UserManagement.Domain.PasskeyAggregate;
using Spamma.Modules.UserManagement.Domain.UserAggregate;
using Xunit;

namespace Spamma.App.Tests;

public class AggregateConstructorTests
{
    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Passkey))]
    [InlineData(typeof(ApiKey))]
    [InlineData(typeof(Domain))]
    [InlineData(typeof(Subdomain))]
    [InlineData(typeof(ChaosAddress))]
    [InlineData(typeof(Campaign))]
    [InlineData(typeof(Email))]
    [InlineData(typeof(CatchAllSenderAddress))]
    public void Aggregate_DoesNotExposeEmptyConstructor(Type aggregateType)
    {
        Assert.Null(aggregateType.GetConstructor(Type.EmptyTypes));
    }

    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Passkey))]
    [InlineData(typeof(ApiKey))]
    [InlineData(typeof(Domain))]
    [InlineData(typeof(Subdomain))]
    [InlineData(typeof(ChaosAddress))]
    [InlineData(typeof(Campaign))]
    [InlineData(typeof(Email))]
    [InlineData(typeof(CatchAllSenderAddress))]
    public void Aggregate_DoesNotExposeReplayMethods(Type aggregateType)
    {
        var methodNames = aggregateType.GetMethods().Select(method => method.Name).ToHashSet();

        Assert.DoesNotContain("Apply", methodNames);
        Assert.DoesNotContain("LoadFromHistory", methodNames);
    }
}
