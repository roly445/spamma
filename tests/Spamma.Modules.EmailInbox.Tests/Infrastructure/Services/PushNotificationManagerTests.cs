using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.EmailInbox.Client.Application.Grpc;
using Spamma.Modules.EmailInbox.Infrastructure.Constants;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Xunit;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class PushNotificationManagerTests
{
    [Fact]
    public async Task NotifyEmailAsync_WithCatchAllCampaignEmail_WritesMetadataToGrpcStream()
    {
        // Arrange
        var stream = new CapturingServerStreamWriter();
        var ownerId = Guid.NewGuid();
        var manager = CreateManager((userId, _) => userId == ownerId);
        await manager.RegisterConnectionAsync("connection-1", stream, ownerId, new TestServerCallContext());

        var emailId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        // Act
        await manager.NotifyEmailAsync(new PushNotificationManager.EmailDetails(
            emailId,
            CatchAllConstants.SubdomainId,
            "sender@example.com",
            "anything@unknown.test",
            "Catch-all campaign",
            "Body",
            DateTimeOffset.UtcNow,
            campaignId,
            "catch-all-campaign",
            IsCatchAll: true,
            DomainId: CatchAllConstants.DomainId));

        // Assert
        stream.Notifications.Should().ContainSingle();
        var notification = stream.Notifications[0];
        notification.Id.Should().Be(emailId.ToString());
        notification.SubdomainId.Should().Be(CatchAllConstants.SubdomainId.ToString());
        notification.IsCatchAll.Should().BeTrue();
        notification.CampaignId.Should().Be(campaignId.ToString());
        notification.CampaignValue.Should().Be("catch-all-campaign");
    }

    [Fact]
    public async Task NotifyEmailAsync_TwoUsersWithDisjointSubdomains_DeliversOnlyToAuthorizedOwner()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var firstSubdomainId = Guid.NewGuid();
        var secondSubdomainId = Guid.NewGuid();
        var firstStream = new CapturingServerStreamWriter();
        var secondStream = new CapturingServerStreamWriter();
        var manager = CreateManager((userId, email) =>
            (userId == firstUserId && email.SubdomainId == firstSubdomainId) ||
            (userId == secondUserId && email.SubdomainId == secondSubdomainId));

        await manager.RegisterConnectionAsync("first", firstStream, firstUserId, new TestServerCallContext());
        await manager.RegisterConnectionAsync("second", secondStream, secondUserId, new TestServerCallContext());

        await manager.NotifyEmailAsync(new PushNotificationManager.EmailDetails(
            Guid.NewGuid(), firstSubdomainId, "sender@example.com", "first@example.com", "First", "Body",
            DateTimeOffset.UtcNow, DomainId: Guid.NewGuid()));
        await manager.NotifyEmailAsync(new PushNotificationManager.EmailDetails(
            Guid.NewGuid(), secondSubdomainId, "sender@example.com", "second@example.com", "Second", "Body",
            DateTimeOffset.UtcNow, Guid.NewGuid(), "campaign", DomainId: Guid.NewGuid()));

        firstStream.Notifications.Select(x => x.Subject).Should().Equal("First");
        secondStream.Notifications.Select(x => x.Subject).Should().Equal("Second");
    }

    private static PushNotificationManager CreateManager(Func<Guid, PushNotificationManager.EmailDetails, bool> canAccess)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmailNotificationAccessService>(new TestEmailNotificationAccessService(canAccess));
        var provider = services.BuildServiceProvider();
        return new PushNotificationManager(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class TestEmailNotificationAccessService(Func<Guid, PushNotificationManager.EmailDetails, bool> canAccess)
        : IEmailNotificationAccessService
    {
        public Task<bool> CanAccessNotificationAsync(Guid userId, PushNotificationManager.EmailDetails email, CancellationToken cancellationToken)
            => Task.FromResult(canAccess(userId, email));

        public Task<bool> CanAccessEmailAsync(Guid userId, Guid emailId, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }

    private sealed class CapturingServerStreamWriter : IServerStreamWriter<EmailNotification>
    {
        public WriteOptions? WriteOptions { get; set; }

        public List<EmailNotification> Notifications { get; } = [];

        public Task WriteAsync(EmailNotification message)
        {
            this.Notifications.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _responseTrailers = [];

        protected override string MethodCore => "SubscribeToEmails";

        protected override string HostCore => "localhost";

        protected override string PeerCore => "test";

        protected override DateTime DeadlineCore => DateTime.MaxValue;

        protected override Metadata RequestHeadersCore => [];

        protected override CancellationToken CancellationTokenCore => CancellationToken.None;

        protected override Metadata ResponseTrailersCore => this._responseTrailers;

        protected override Status StatusCore { get; set; }

        protected override WriteOptions? WriteOptionsCore { get; set; }

        protected override AuthContext AuthContextCore => new("anonymous", []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
            => Task.CompletedTask;
    }
}
