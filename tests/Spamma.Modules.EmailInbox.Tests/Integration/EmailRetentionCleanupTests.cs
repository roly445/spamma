using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spamma.Modules.Common.Domain.Contracts;
using Spamma.Modules.Common.IntegrationEvents.EmailInbox;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Client.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate.Events;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Repositories;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Services.Retention;
using Spamma.Modules.EmailInbox.Tests.Fixtures;
using Xunit;
using EmailAggregate = Spamma.Modules.EmailInbox.Domain.EmailAggregate.Email;

namespace Spamma.Modules.EmailInbox.Tests.Integration;

public class EmailRetentionCleanupTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task CleanupOnceAsync_AppliesPerInboxCountAndAgeWhilePreservingFavoritesAndCampaigns()
    {
        var session = fixture.Session!;
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var domainId = Guid.NewGuid();
        var countInbox = Guid.NewGuid();
        var ageInbox = Guid.NewGuid();
        var oldestCount = Guid.NewGuid();
        var newestCount = Guid.NewGuid();
        var middleCount = Guid.NewGuid();
        var favorite = Guid.NewGuid();
        var campaign = Guid.NewGuid();
        var oldAge = Guid.NewGuid();
        var newAge = Guid.NewGuid();

        session.Store(new EmailRetentionPolicy(EmailRetentionPolicy.DomainKey(domainId), EmailRetentionMode.Override, 24, null));
        session.Store(new EmailRetentionPolicy(EmailRetentionPolicy.SubdomainKey(countInbox), EmailRetentionMode.Override, null, 2));
        Seed(session, oldestCount, domainId, countInbox, now.AddHours(-4));
        Seed(session, middleCount, domainId, countInbox, now.AddHours(-3));
        Seed(session, newestCount, domainId, countInbox, now.AddHours(-2));
        Seed(session, favorite, domainId, countInbox, now.AddHours(-10), favorite: true);
        Seed(session, campaign, domainId, countInbox, now.AddHours(-10), campaign: true);
        Seed(session, oldAge, domainId, ageInbox, now.AddHours(-25));
        Seed(session, newAge, domainId, ageInbox, now.AddHours(-23));
        await session.SaveChangesAsync();

        var deleted = new List<Guid>();
        var publisher = new Mock<IIntegrationEventPublisher>();
        publisher.Setup(x => x.PublishAsync(It.IsAny<EmailDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<EmailDeletedIntegrationEvent, CancellationToken>((ev, _) => deleted.Add(ev.EmailId))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(session);
        services.AddScoped<IEmailRepository>(provider => new EmailRepository(
            provider.GetRequiredService<IDocumentSession>(), Mock.Of<IHostEnvironment>()));
        services.AddScoped<EmailRetentionPolicyService>();
        services.AddSingleton(publisher.Object);
        using var provider = services.BuildServiceProvider();
        var worker = new EmailCleanupBackgroundService(
            provider,
            NullLogger<EmailCleanupBackgroundService>.Instance, new StubTimeProvider(now));

        await worker.CleanupOnceAsync(CancellationToken.None);

        deleted.Should().BeEquivalentTo([oldestCount, oldAge]);
        var deletedLookups = await session.Query<EmailLookup>().Where(x => x.DeletedAt != null).Select(x => x.Id).ToListAsync();
        deletedLookups.Should().BeEquivalentTo([oldestCount, oldAge]);
        var searchBodies = await session.Query<EmailLookup>().Where(x => deleted.Contains(x.Id))
            .Select(x => x.SearchableBody!).ToListAsync();
        searchBodies.Should().OnlyContain(x => x == string.Empty);
    }

    private static void Seed(IDocumentSession session, Guid emailId, Guid domainId, Guid subdomainId,
        DateTimeOffset sentAt, bool favorite = false, bool campaign = false)
    {
        var events = new List<object>
        {
            new EmailReceived(emailId, domainId, subdomainId, "Retention fixture", sentAt, [],
                SearchableBody: "retention indexed text"),
        };
        if (favorite)
        {
            events.Add(new EmailMarkedAsFavorite(sentAt.UtcDateTime));
        }

        if (campaign)
        {
            events.Add(new CampaignCaptured(Guid.NewGuid()));
        }

        session.Events.StartStream<EmailAggregate>(emailId, events.ToArray());
    }
}
