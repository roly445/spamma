using System.Text.Json;
using Dapper;
using FluentAssertions;
using MaybeMonad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MimeKit;
using Npgsql;
using ResultMonad;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;
using Spamma.Modules.EmailInbox.Tests.E2E.Helpers;

namespace Spamma.Modules.EmailInbox.Tests.E2E;

[Collection("SmtpE2E")]
public class SmtpDurabilityTests
{
    [Fact]
    public async Task AcceptedMessage_SurvivesHostRestartBeforeSubscription()
    {
        var fixture = new SmtpEndToEndFixture();
        try
        {
            await fixture.InitializeAsync(enableSubscriber: false);
            var subject = $"Restart capture {Guid.NewGuid():N}";
            var body = $"Original MIME body {Guid.NewGuid():N}";
            var client = new SmtpClientHelper("localhost", fixture.SmtpServerPort);

            var (accepted, response) = await client.TrySendEmailAsync(
                "sender@external.com", "restart@spamma.example.com", subject, body);

            accepted.Should().BeTrue(response);
            await using (var connection = new NpgsqlConnection(fixture.PostgresContainer.GetConnectionString()))
            {
                var publishedCount = await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM cap.published");
                publishedCount.Should().Be(1, "SMTP acknowledgment must follow durable CAP persistence");
                var receivedCount = await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM cap.received");
                receivedCount.Should().Be(0, "the first host has no subscriber");
            }

            await fixture.RestartAsync();
            var email = await fixture.WaitForEmailAsync(subject);
            using var scope = fixture.ServiceProvider.CreateScope();
            var mime = await scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>()
                .LoadMessageContentAsync(email.Id);
            mime.HasValue.Should().BeTrue();
            mime.Value.TextBody.Should().Be(body);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task CampaignCapture_RetriesWorkerFailure_WithoutDuplicateCount()
    {
        var fixture = new SmtpEndToEndFixture();
        var failure = new MessageStoreFailure(failuresToInject: 1);
        try
        {
            await fixture.InitializeAsync(enableSubscriber: true, retryCount: 3,
                configureServices: services => ConfigureFailingMessageStore(services, failure));
            var subject = $"Retry campaign {Guid.NewGuid():N}";
            var body = $"Original campaign body {Guid.NewGuid():N}";
            var campaignValue = $"Retry-{Guid.NewGuid():N}";
            var client = new SmtpClientHelper("localhost", fixture.SmtpServerPort);

            var response = await client.SendEmailAsync(
                "campaign@external.com", "retry@spamma.example.com", subject, body,
                new Dictionary<string, string> { ["X-Spamma-Camp"] = campaignValue });

            response.Should().Be("Ok");
            var email = await fixture.WaitForEmailAsync(subject);
            var campaign = await fixture.WaitForCampaignAsync(campaignValue);
            failure.Attempts.Should().BeGreaterThanOrEqualTo(2, "CAP should retry the injected worker failure");
            campaign.TotalCaptured.Should().Be(1, "replay of the same message must not count twice");
            campaign.SampleMessageId.Should().Be(email.Id);
            email.CampaignId.Should().Be(campaign.CampaignId);

            using var scope = fixture.ServiceProvider.CreateScope();
            var mime = await scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>()
                .LoadMessageContentAsync(email.Id);
            mime.HasValue.Should().BeTrue();
            mime.Value.TextBody.Should().Be(body);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task ExhaustedRetries_LeaveInspectableFailureDetails()
    {
        var fixture = new SmtpEndToEndFixture();
        var failure = new MessageStoreFailure(failuresToInject: int.MaxValue);
        try
        {
            await fixture.InitializeAsync(enableSubscriber: true, retryCount: 1,
                configureServices: services => ConfigureFailingMessageStore(services, failure));
            var client = new SmtpClientHelper("localhost", fixture.SmtpServerPort);
            var subject = $"Exhausted retry {Guid.NewGuid():N}";

            var (accepted, response) = await client.TrySendEmailAsync(
                "sender@external.com", "failure@spamma.example.com", subject, "Original body");

            accepted.Should().BeTrue(response);
            var receipt = await WaitForFailedReceiptAsync(fixture.PostgresContainer.GetConnectionString());
            receipt.Retries.Should().Be(1, "the configured retry limit should be exhausted");
            receipt.Error.Should().Contain("Injected MIME storage failure");
            receipt.Error.Should().Contain(receipt.MessageId);
            failure.Attempts.Should().BeGreaterThanOrEqualTo(1);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static async Task<FailedReceipt> WaitForFailedReceiptAsync(string connectionString)
    {
        var timeout = DateTime.UtcNow.AddSeconds(40);
        string? latest = null;
        while (DateTime.UtcNow < timeout)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            latest = await connection.QuerySingleOrDefaultAsync<string>(
                "SELECT row_to_json(receipt)::text FROM cap.received receipt LIMIT 1");
            if (latest != null)
            {
                using var receipt = JsonDocument.Parse(latest);
                var root = receipt.RootElement;
                var retries = root.GetProperty("Retries").GetInt32();
                if (root.GetProperty("StatusName").GetString() == "Failed" && retries >= 1)
                {
                    using var content = JsonDocument.Parse(root.GetProperty("Content").GetString()!);
                    var error = content.RootElement.GetProperty("Headers").GetProperty("cap-exception").GetString();
                    var messageId = content.RootElement.GetProperty("Value").GetProperty("MessageId").GetString();
                    return new FailedReceipt(retries, error ?? string.Empty, messageId ?? string.Empty);
                }
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"CAP did not retain an inspectable failure. Last receipt: {latest}");
    }

    private static void ConfigureFailingMessageStore(IServiceCollection services, MessageStoreFailure failure)
    {
        services.AddSingleton<LocalMessageStoreProvider>();
        services.RemoveAll<IMessageStoreProvider>();
        services.AddSingleton<IMessageStoreProvider>(provider => new FailingMessageStore(
            provider.GetRequiredService<LocalMessageStoreProvider>(), failure));
    }

    private sealed record FailedReceipt(int Retries, string Error, string MessageId);

    private sealed class MessageStoreFailure(int failuresToInject)
    {
        private int attempts;

        public int Attempts => Volatile.Read(ref this.attempts);

        public bool ShouldFail() => Interlocked.Increment(ref this.attempts) <= failuresToInject;
    }

    private sealed class FailingMessageStore(IMessageStoreProvider inner, MessageStoreFailure failure)
        : IMessageStoreProvider
    {
        public ValueTask<Result> StoreMessageContentAsync(
            Guid messageId, MimeMessage messageContent,
            CancellationToken cancellationToken = default)
        {
            if (failure.ShouldFail())
            {
                throw new InvalidOperationException("Injected MIME storage failure");
            }

            return inner.StoreMessageContentAsync(messageId, messageContent, cancellationToken);
        }

        public ValueTask<Result> DeleteMessageContentAsync(
            Guid messageId,
            CancellationToken cancellationToken = default) =>
            inner.DeleteMessageContentAsync(messageId, cancellationToken);

        public ValueTask<Maybe<MimeMessage>> LoadMessageContentAsync(
            Guid messageId,
            CancellationToken cancellationToken = default) =>
            inner.LoadMessageContentAsync(messageId, cancellationToken);
    }
}
