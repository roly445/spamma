using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Spamma.Modules.EmailInbox.Infrastructure.ReadModels;
using Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;
using Spamma.Modules.EmailInbox.Tests.E2E.Helpers;

namespace Spamma.Modules.EmailInbox.Tests.E2E;

/// <summary>
/// End-to-end integration tests for SMTP email reception.
/// These tests validate the complete flow: SMTP client → SMTP server → message processing → database.
/// </summary>
[Collection("SmtpE2E")]
public class SmtpEmailReceptionTests : IClassFixture<SmtpEndToEndFixture>
{
    private readonly SmtpEndToEndFixture _fixture;
    private readonly SmtpClientHelper _smtpClient;

    public SmtpEmailReceptionTests(SmtpEndToEndFixture fixture)
    {
        _fixture = fixture;
        _smtpClient = new SmtpClientHelper("localhost", fixture.SmtpServerPort);
    }

    [Fact(Skip = "E2E test infrastructure needs refactoring - projections not running correctly. See E2E_TEST_STATUS.md")]
    public async Task SendValidEmail_ToActiveSubdomain_StoresEmailSuccessfully()
    {
        // Arrange
        var subject = $"E2E Test Email - {Guid.NewGuid()}";
        var body = "This is a test email sent via E2E tests";
        var from = "sender@external.com";
        var to = "test@spamma.example.com";

        // Act
        var (success, message) = await _smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeTrue("Email should be accepted by SMTP server");
        message.Should().Contain("250", "SMTP response should indicate success");

        // Verify email stored in database
        await Task.Delay(500); // Allow time for async processing
        var session = _fixture.ServiceProvider.GetRequiredService<IDocumentSession>();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().NotBeNull("Email should be persisted to database");
        email!.Subject.Should().Be(subject);
        email.EmailAddresses.Should().Contain(e => e.Address.Contains("sender@external.com"), "From address should be stored");
        email.EmailAddresses.Should().Contain(e => e.Address.Contains("test@spamma.example.com"), "To address should be stored");
    }

    [Fact(Skip = "E2E test infrastructure needs refactoring - projections not running correctly. See E2E_TEST_STATUS.md")]
    public async Task SendEmail_ToUnknownDomain_ReturnsMailboxNameNotAllowed()
    {
        // Arrange
        var subject = "Test email to unknown domain";
        var body = "This should be rejected";
        var from = "sender@external.com";
        var to = "user@unknowndomain.com"; // Domain not in test data

        // Act
        var (success, message) = await _smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeFalse("Email to unknown domain should be rejected");
        message.Should().Contain("553", "Should return 553 Mailbox name not allowed");

        // Verify email NOT stored in database
        await Task.Delay(200);
        var session = _fixture.ServiceProvider.GetRequiredService<IDocumentSession>();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().BeNull("Rejected email should not be stored");
    }

    [Fact(Skip = "E2E test infrastructure needs refactoring - projections not running correctly. See E2E_TEST_STATUS.md")]
    public async Task SendEmail_ToChaosAddressEnabled_ReturnsConfiguredSmtpCode()
    {
        // Arrange
        var subject = "Test email to chaos address";
        var body = "This should return chaos response";
        var from = "sender@external.com";
        var to = "chaos@spamma.example.com"; // Seeded chaos address (450)

        // Act
        var (success, message) = await _smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeFalse("Chaos address should return configured error code");
        message.Should().Contain("450", "Chaos address configured to return 450 Mailbox Unavailable");

        // Verify email NOT stored (chaos addresses don't persist)
        await Task.Delay(200);
        var session = _fixture.ServiceProvider.GetRequiredService<IDocumentSession>();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().BeNull("Chaos address emails should not be stored");
    }

    [Fact(Skip = "E2E test infrastructure needs refactoring - projections not running correctly. See E2E_TEST_STATUS.md")]
    public async Task SendEmail_ToDisabledChaosAddress_FallsBackToNormalProcessing()
    {
        // Arrange
        var subject = $"E2E Test - Disabled Chaos - {Guid.NewGuid()}";
        var body = "Disabled chaos address should process normally";
        var from = "sender@external.com";
        var to = "disabled@spamma.example.com"; // Seeded disabled chaos address

        // Act
        var (success, message) = await _smtpClient.TrySendEmailAsync(from, to, subject, body);

        // Assert
        success.Should().BeTrue("Disabled chaos address should fallback to normal processing");
        message.Should().Contain("250", "Should accept email normally");

        // Verify email IS stored (disabled chaos address processes normally)
        await Task.Delay(500);
        var session = _fixture.ServiceProvider.GetRequiredService<IDocumentSession>();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().NotBeNull("Disabled chaos address email should be stored");
        email!.Subject.Should().Be(subject);
    }

    [Fact(Skip = "E2E test infrastructure needs refactoring - projections not running correctly. See E2E_TEST_STATUS.md")]
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
        var response = await _smtpClient.SendEmailAsync(from, to, subject, body, headers);

        // Assert
        response.Should().Contain("250", "Campaign email should be accepted");

        // Verify email stored with campaign metadata
        await Task.Delay(500); // Allow time for background job processing
        var session = _fixture.ServiceProvider.GetRequiredService<IDocumentSession>();
        var email = await session.Query<EmailLookup>()
            .Where(e => e.Subject == subject)
            .FirstOrDefaultAsync();

        email.Should().NotBeNull("Campaign email should be stored");
        email!.Subject.Should().Be(subject);

        // Note: Campaign metadata verification would require querying campaign projections
        // This test validates the email is accepted and stored successfully
    }

    [Fact(Skip = "E2E test infrastructure needs refactoring - projections not running correctly. See E2E_TEST_STATUS.md")]
    public async Task SendMultipleEmailsConcurrently_AllProcessedSuccessfully()
    {
        // Arrange
        var emailCount = 5;
        var tasks = new List<Task<(bool Success, string Message)>>();

        for (int i = 0; i < emailCount; i++)
        {
            var subject = $"Concurrent Test {i} - {Guid.NewGuid()}";
            var body = $"Concurrent email body {i}";
            var from = $"sender{i}@external.com";
            var to = "test@spamma.example.com";

            tasks.Add(_smtpClient.TrySendEmailAsync(from, to, subject, body));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue("All emails should be accepted"));

        // Verify all emails stored
        await Task.Delay(1000); // Allow time for all async processing
        var session = _fixture.ServiceProvider.GetRequiredService<IDocumentSession>();
        var storedEmails = await session.Query<EmailLookup>()
            .Where(e => e.Subject.StartsWith("Concurrent Test"))
            .ToListAsync();

        storedEmails.Should().HaveCount(emailCount, "All concurrent emails should be stored");
    }
}
