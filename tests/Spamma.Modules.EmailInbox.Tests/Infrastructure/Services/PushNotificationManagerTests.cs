using FluentAssertions;
using Grpc.Core;
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
        var manager = new PushNotificationManager();
        await manager.RegisterConnectionAsync("connection-1", stream, Guid.Empty, new TestServerCallContext());

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
            IsCatchAll: true));

        // Assert
        stream.Notifications.Should().ContainSingle();
        var notification = stream.Notifications[0];
        notification.Id.Should().Be(emailId.ToString());
        notification.SubdomainId.Should().Be(CatchAllConstants.SubdomainId.ToString());
        notification.IsCatchAll.Should().BeTrue();
        notification.CampaignId.Should().Be(campaignId.ToString());
        notification.CampaignValue.Should().Be("catch-all-campaign");
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
