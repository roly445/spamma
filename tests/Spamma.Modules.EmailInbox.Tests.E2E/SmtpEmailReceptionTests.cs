using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;
using Spamma.Modules.EmailInbox.Tests.E2E.Helpers;

namespace Spamma.Modules.EmailInbox.Tests.E2E;

[Collection("SmtpE2E")]
public class SmtpEmailReceptionTests : IClassFixture<SmtpEndToEndFixture>
{
    private readonly SmtpEndToEndFixture _fixture;
    private readonly SmtpClientHelper _smtpClient;

    public SmtpEmailReceptionTests(SmtpEndToEndFixture fixture)
    {
        this._fixture = fixture;
        this._smtpClient = new SmtpClientHelper("localhost", fixture.SmtpServerPort);
    }

    [Fact]
    public async Task SendValidEmail_ToActiveSubdomain_StoresEmailSuccessfully()
    {
        // Arrange
        var subject = $"E2E Test Email - {Guid.NewGuid()}";
        var body = "This is a test email sent via E2E tests";
        var from = "sender@external.com";
        var to = "test@spamma.example.com";

        // Act
        var (success, message) = await this._smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeTrue("Email should be accepted by SMTP server");
        message.Should().Be("Ok", "MailKit returns the SMTP success text after a 250 response");

        // Verify email stored in database
        var email = await this._fixture.WaitForEmailAsync(subject);

        email.Should().NotBeNull("Email should be persisted to database");
        email!.Subject.Should().Be(subject);
        email.EmailAddresses.Should().Contain(e => e.Address.Contains("sender@external.com"), "From address should be stored");
        email.EmailAddresses.Should().Contain(e => e.Address.Contains("test@spamma.example.com"), "To address should be stored");

        using var scope = this._fixture.ServiceProvider.CreateScope();
        var storedMime = await scope.ServiceProvider.GetRequiredService<IMessageStoreProvider>()
            .LoadMessageContentAsync(email.Id);
        storedMime.HasValue.Should().BeTrue("the CAP subscriber should retain the original MIME content");
        storedMime.Value.TextBody.Should().Be(body);
    }

    [Fact]
    public async Task SendEmail_ToUnknownDomain_ReturnsMailboxNameNotAllowed()
    {
        // Arrange
        var subject = $"Test email to unknown domain - {Guid.NewGuid()}";
        var body = "This should be rejected";
        var from = "sender@external.com";
        var to = "user@unknowndomain.com"; // Domain not in test data

        // Act
        var (success, message) = await this._smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeFalse("Email to unknown domain should be rejected");
        message.Should().Contain("553", "Should return 553 Mailbox name not allowed");

        // Verify email NOT stored in database
        await using var session = this._fixture.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().BeNull("Rejected email should not be stored");
    }

    [Fact]
    public async Task SendEmail_ToChaosAddressEnabled_ReturnsConfiguredSmtpCode()
    {
        // Arrange
        var subject = $"Test email to chaos address - {Guid.NewGuid()}";
        var body = "This should return chaos response";
        var from = "sender@external.com";
        var to = "chaos@spamma.example.com"; // Seeded chaos address (450)

        // Act
        var (success, message) = await this._smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeFalse("Chaos address should return configured error code");
        message.Should().Contain("450", "Chaos address configured to return 450 Mailbox Unavailable");
        var chaosAddress = await this._fixture.WaitForChaosCaptureAsync();
        chaosAddress.TotalReceived.Should().Be(1, "the rejected message is still captured by the CAP subscriber");

        // Verify email NOT stored (chaos addresses don't persist)
        await using var session = this._fixture.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().BeNull("Chaos address emails should not be stored");
    }

    [Fact]
    public async Task RetrySameCampaignMessage_ToTemporaryChaosAddress_CountsOneFailureAndTwoAttempts()
    {
        var campaignValue = $"bounce-{Guid.NewGuid()}";
        var subject = $"Bounce retry {Guid.NewGuid()}";
        var message = new MimeMessage
        {
            From = { MailboxAddress.Parse("sender@external.com") },
            To = { MailboxAddress.Parse("chaos@spamma.example.com") },
            Subject = subject,
            Body = new TextPart("plain") { Text = "retry this identical message" },
        };
        message.Headers.Add("X-Spamma-Camp", campaignValue);

        var first = await this._smtpClient.TrySendMessageAsync(message);
        var second = await this._smtpClient.TrySendMessageAsync(message);

        first.Message.Should().Contain("450");
        second.Message.Should().Contain("450");

        CampaignSummary? campaign = null;
        var deadline = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < deadline)
        {
            await using var query = this._fixture.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
            campaign = await query.Query<CampaignSummary>().FirstOrDefaultAsync(x => x.CampaignValue == campaignValue);
            if (campaign?.FailureDeliveryAttempts == 2)
            {
                break;
            }

            await Task.Delay(200);
        }

        campaign.Should().NotBeNull();
        campaign!.TemporaryFailureMessages.Should().Be(1);
        campaign.PermanentFailureMessages.Should().Be(0);
        campaign.FailureDeliveryAttempts.Should().Be(2);
        campaign.TotalCaptured.Should().Be(0);
        campaign.SampleMessageId.Should().BeNull();

        await using var session = this._fixture.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
        (await session.Query<EmailLookup>().AnyAsync(x => x.Subject == subject)).Should().BeFalse();
    }

    [Fact]
    public async Task CampaignFailures_SeparateTemporaryAndPermanentMessages_FromCapturedMail()
    {
        var campaignValue = $"mixed-bounce-{Guid.NewGuid()}";
        var softSubject = $"soft-{Guid.NewGuid()}";
        var distinctSoftSubject = $"another-soft-{Guid.NewGuid()}";
        var hardSubject = $"hard-{Guid.NewGuid()}";
        var acceptedSubject = $"accepted-{Guid.NewGuid()}";

        static MimeMessage Create(string to, string subject, string campaign)
        {
            var message = new MimeMessage
            {
                From = { MailboxAddress.Parse("sender@external.com") },
                To = { MailboxAddress.Parse(to) },
                Subject = subject,
                Body = new TextPart("plain") { Text = $"body for {subject}" },
            };
            message.Headers.Add("X-Spamma-Camp", campaign);
            return message;
        }

        var soft = await this._smtpClient.TrySendMessageAsync(Create("chaos@spamma.example.com", softSubject, campaignValue));
        soft.Message.Should().Contain("450");
        var distinctSoft = await this._smtpClient.TrySendMessageAsync(Create("chaos@spamma.example.com", distinctSoftSubject, campaignValue));
        distinctSoft.Message.Should().Contain("450");
        var hard = await this._smtpClient.TrySendMessageAsync(Create("permanent@spamma.example.com", hardSubject, campaignValue));
        hard.Message.Should().Contain("550");

        var deadline = DateTime.UtcNow.AddSeconds(40);
        CampaignSummary? campaign = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var query = this._fixture.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
            campaign = await query.Query<CampaignSummary>().FirstOrDefaultAsync(x => x.CampaignValue == campaignValue);
            if (campaign is { TemporaryFailureMessages: 2, PermanentFailureMessages: 1 })
            {
                break;
            }

            await Task.Delay(200);
        }

        campaign.Should().NotBeNull();
        campaign!.TotalCaptured.Should().Be(0);
        campaign.FailureDeliveryAttempts.Should().Be(3);

        (await this._smtpClient.TrySendMessageAsync(Create("test@spamma.example.com", acceptedSubject, campaignValue)))
            .Success.Should().BeTrue();
        var captured = await this._fixture.WaitForEmailAsync(acceptedSubject);
        captured.CampaignId.Should().Be(campaign.CampaignId);

        await using var session = this._fixture.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
        campaign = await session.Query<CampaignSummary>().FirstOrDefaultAsync(x => x.CampaignValue == campaignValue);
        campaign!.TotalCaptured.Should().Be(1);
        campaign.SampleMessageId.Should().Be(captured.Id);
        (await session.Query<EmailLookup>().CountAsync(x => x.Subject == softSubject || x.Subject == distinctSoftSubject || x.Subject == hardSubject))
            .Should().Be(0);
    }

    [Fact]
    public async Task SendEmail_ToDisabledChaosAddress_FallsBackToNormalProcessing()
    {
        // Arrange
        var subject = $"E2E Test - Disabled Chaos - {Guid.NewGuid()}";
        var body = "Disabled chaos address should process normally";
        var from = "sender@external.com";
        var to = "disabled@spamma.example.com"; // Seeded disabled chaos address

        // Act
        var (success, message) = await this._smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeTrue("Disabled chaos address should fallback to normal processing");
        message.Should().Be("Ok", "MailKit returns the SMTP success text");

        // Verify email IS stored (disabled chaos address processes normally)
        var email = await this._fixture.WaitForEmailAsync(subject);

        email.Should().NotBeNull("Disabled chaos address email should be stored");
        email!.Subject.Should().Be(subject);
    }

    [Fact]
    public async Task SendEmail_WithCampaignHeader_StoresEmailWithCampaignMetadata()
    {
        // Arrange
        var subject = $"E2E Campaign Test - {Guid.NewGuid()}";
        var body = "Campaign email test";
        var from = "campaign@external.com";
        var to = "test@spamma.example.com";
        var campaignValue = $"TestCampaign-{Guid.NewGuid()}";

        var headers = new Dictionary<string, string>
        {
            { "X-Spamma-Camp", campaignValue },
        };

        // Act
        var response = await this._smtpClient.SendEmailAsync(from, to, subject, body, headers);

        // Assert
        response.Should().Be("Ok", "Campaign email should be accepted");

        // Verify email stored with campaign metadata
        var email = await this._fixture.WaitForEmailAsync(subject);

        email.Should().NotBeNull("Campaign email should be stored");
        email!.Subject.Should().Be(subject);
        var campaign = await this._fixture.WaitForCampaignAsync(campaignValue);
        email.CampaignId.Should().Be(campaign.CampaignId);
        campaign.CampaignValue.Should().Be(campaignValue);
        campaign.SampleMessageId.Should().Be(email.Id);
        campaign.TotalCaptured.Should().Be(1);
    }

    [Fact]
    public async Task SendMultipleEmailsConcurrently_AllProcessedSuccessfully()
    {
        // Arrange
        var emailCount = 5;
        var tasks = new List<Task<(bool Success, string Message)>>();
        var subjects = new List<string>();

        for (int i = 0; i < emailCount; i++)
        {
            var subject = $"Concurrent Test {i} - {Guid.NewGuid()}";
            subjects.Add(subject);
            var body = $"Concurrent email body {i}";
            var from = $"sender{i}@external.com";
            var to = "test@spamma.example.com";

            tasks.Add(this._smtpClient.TrySendEmailAsync(from, to, subject, body));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue("All emails should be accepted"));

        // Verify all emails stored
        var emails = await Task.WhenAll(subjects.Select(this._fixture.WaitForEmailAsync));
        emails.Select(email => email.Id).Should().OnlyHaveUniqueItems();
        emails.Select(email => email.Subject).Should().BeEquivalentTo(subjects);
    }
}
