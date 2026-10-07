using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using MimeKit;
using Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.SpamFeedback;

public sealed class SpamFeedbackTests
{
    [Fact]
    public void ArfReportContainsFeedbackFieldsAndOriginalMessage()
    {
        var original = new MimeMessage();
        original.From.Add(MailboxAddress.Parse("sender@example.net"));
        original.To.Add(MailboxAddress.Parse("receiver@inbound.example.com"));
        original.Subject = "A suspicious offer";
        original.Body = new TextPart("plain") { Text = "Original body" };
        var report = CreateReport(arfRecipient: "abuse@example.com");

        var message = ArfFeedbackSender.BuildReport(report, original, "reports@example.com", "Spamma");

        var body = Assert.IsType<MultipartReport>(message.Body);
        Assert.Equal("feedback-report", body.ContentType.Parameters["report-type"]);
        Assert.Equal(3, body.Count);
        Assert.IsType<TextPart>(body[0]);
        var feedback = Assert.IsType<MimePart>(body[1]);
        Assert.Equal("message/feedback-report", feedback.ContentType.MimeType);
        using var buffer = new MemoryStream();
        Assert.NotNull(feedback.Content);
        feedback.Content.DecodeTo(buffer);
        var fields = Encoding.ASCII.GetString(buffer.ToArray());
        Assert.Contains("Feedback-Type: abuse\r\n", fields);
        Assert.Contains("User-Agent: Spamma/1.0\r\n", fields);
        Assert.Contains("Version: 1\r\n", fields);
        Assert.Contains("Original-Mail-From: <sender@example.net>\r\n", fields);
        Assert.Contains("Original-Rcpt-To: <receiver@inbound.example.com>\r\n", fields);
        Assert.Same(original, Assert.IsType<MessagePart>(body[2]).Message);
    }

    [Theory]
    [InlineData("http://feedback.example.com/spam")]
    [InlineData("https://localhost/spam")]
    [InlineData("https://127.0.0.1/spam")]
    [InlineData("https://feedback.example.com:8443/spam")]
    [InlineData("https://feedback.example.com/spam#fragment")]
    public void WebhookRejectsUnsafeUrls(string url) => Assert.False(WebhookDestinationPolicy.IsAllowedUrl(url));

    [Fact]
    public void WebhookAcceptsOnlyParentDomainAndPublicAddresses()
    {
        Assert.True(WebhookDestinationPolicy.IsUnderParentDomain("https://feedback.example.com/spam", "example.com"));
        Assert.False(WebhookDestinationPolicy.IsUnderParentDomain("https://example.com.attacker.net/spam", "example.com"));
        Assert.True(WebhookDestinationPolicy.IsPublicAddress(IPAddress.Parse("8.8.8.8")));
        Assert.False(WebhookDestinationPolicy.IsPublicAddress(IPAddress.Parse("10.0.0.1")));
        Assert.False(WebhookDestinationPolicy.IsPublicAddress(IPAddress.IPv6Loopback));
    }

    [Fact]
    public async Task WebhookSignsTheExactBodyAndIncludesCampaignContext()
    {
        var protection = new EphemeralDataProtectionProvider();
        var secret = RandomNumberGenerator.GetBytes(32);
        var handler = new RecordingHandler();
        var sender = new WebhookFeedbackSender(new HttpClient(handler), protection);
        var report = CreateReport() with
        {
            WebhookUrl = "https://feedback.example.com/spam",
            ProtectedWebhookSecret = protection.CreateProtector("Spamma.SpamFeedback.WebhookSecret.v1")
                .Protect(Convert.ToHexString(secret)),
            CampaignId = Guid.NewGuid(),
            CampaignValue = "autumn-test",
        };

        await sender.SendAsync(report, CancellationToken.None);

        Assert.Equal(report.Id.ToString(), handler.ReportId);
        Assert.Equal(report.Id.ToString(), handler.IdempotencyKey);
        Assert.Contains("\"campaignValue\":\"autumn-test\"", handler.Body);
        var expected = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes($"{handler.Timestamp}.{handler.Body}"));
        Assert.Equal($"sha256={Convert.ToHexString(expected).ToLowerInvariant()}", handler.Signature);
    }

    private static SpamReport CreateReport(string? arfRecipient = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null,
        "receiver@inbound.example.com", "sender@example.net", SpamReportTrigger.Manual,
        DateTimeOffset.UtcNow, arfRecipient, null, null,
        new FeedbackChannelState(FeedbackDeliveryStatus.Disabled),
        new FeedbackChannelState(FeedbackDeliveryStatus.Disabled), null);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        public string Timestamp { get; private set; } = string.Empty;

        public string Signature { get; private set; } = string.Empty;

        public string ReportId { get; private set; } = string.Empty;

        public string IdempotencyKey { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            this.Timestamp = request.Headers.GetValues("X-Spamma-Timestamp").Single();
            this.Signature = request.Headers.GetValues("X-Spamma-Signature").Single();
            this.ReportId = request.Headers.GetValues("X-Spamma-Report-Id").Single();
            this.IdempotencyKey = request.Headers.GetValues("Idempotency-Key").Single();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
