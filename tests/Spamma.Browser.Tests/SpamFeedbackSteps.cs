using System.Text.Json;
using Marten;
using Microsoft.Playwright;
using MimeKit;
using Reqnroll;
using Spamma.Modules.EmailInbox.Infrastructure.SpamFeedback;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private string feedbackSubject = string.Empty;
    private string feedbackCampaign = string.Empty;
    private Guid feedbackEmailId;
    private SpamReport? feedbackReport;
    private MimeMessage? arfMessage;
    private (bool Accepted, int Code, string Response) feedbackSmtpResult;

    [AfterScenario("spam-feedback")]
    public async Task DisposeFeedbackFixturesAsync()
    {
        if (this.chaosFixture is not null) await this.chaosFixture.DisposeAsync();
        if (this.inboxFixture is not null) await this.inboxFixture.DisposeAsync();
        if (this.smtpFixture is not null) await this.smtpFixture.DisposeAsync();
    }

    [Given("I moderate a verified feedback subdomain")]
    public Task GivenIModerateFeedbackSubdomainAsync() => this.PrepareChaosAsync();

    [Given("I can only view a feedback subdomain")]
    public Task GivenIViewFeedbackSubdomainAsync() => this.PrepareChaosAsync(viewer: true);

    [When("I open its feedback settings")]
    public Task WhenIOpenFeedbackSettingsAsync() =>
        this.Page.GotoAsync($"/admin/subdomains/{this.chaosSubdomainId}/spam-feedback");

    [When("I save its ARF and webhook destinations")]
    public async Task WhenISaveFeedbackDestinationsAsync()
    {
        await this.WhenIOpenFeedbackSettingsAsync();
        await this.Page.GetByLabel("ARF email destination").FillAsync($"abuse@{this.chaosDomainName}");
        await this.Page.GetByLabel("Signed webhook URL").FillAsync($"https://feedback.{this.chaosDomainName}/spam");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save feedback settings" }).ClickAsync();
    }

    [Then("I see the one-time webhook signing secret")]
    public async Task ThenISeeOneTimeWebhookSecretAsync()
    {
        await Assertions.Expect(this.Page.Locator("#webhook-secret")).ToBeVisibleAsync();
        Assert.Equal(64, (await this.Page.Locator("#webhook-secret").InnerTextAsync()).Length);
        await this.Page.ReloadAsync();
        await Assertions.Expect(this.Page.GetByText("A webhook signing secret is configured.", new() { Exact = false })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#webhook-secret")).ToHaveCountAsync(0);
    }

    [Then("I can disable ARF without disabling the webhook")]
    public async Task ThenICanDisableArfAsync()
    {
        await this.Page.GetByLabel("ARF email destination").FillAsync(string.Empty);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save feedback settings" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Status).Last).ToContainTextAsync("Feedback settings saved");
        await this.Page.ReloadAsync();
        await Assertions.Expect(this.Page.GetByLabel("ARF email destination")).ToHaveValueAsync(string.Empty);
        await Assertions.Expect(this.Page.GetByLabel("Signed webhook URL"))
            .ToHaveValueAsync($"https://feedback.{this.chaosDomainName}/spam");
    }

    [Then("feedback configuration is denied")]
    public async Task ThenFeedbackConfigurationIsDeniedAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Alert))
            .ToContainTextAsync("You do not have permission to configure feedback");
        await Assertions.Expect(this.Page.GetByLabel("ARF email destination")).ToHaveCountAsync(0);
    }

    [Given("I am viewing a received message for feedback")]
    public async Task GivenIViewReceivedFeedbackMessageAsync()
    {
        await this.PrepareInboxAsync(f => this.SeedOneMessageAsync(f));
        await this.OpenFixtureMessageAsync();
    }

    [When("I report the message as spam twice")]
    public async Task WhenIReportTheMessageTwiceAsync()
    {
        await this.Page.Locator("#report-spam").ClickAsync();
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToBeVisibleAsync();
        var response = await this.Page.Context.APIRequest.PostAsync(
            $"/api/email-inbox/emails/{this.inboxMessageId}/spam-report");
        Assert.True(response.Ok);
        using var json = JsonDocument.Parse(await response.TextAsync());
        Assert.Equal(this.inboxMessageId, json.RootElement.GetProperty("reportId").GetGuid());
    }

    [Then("one manual report is shown for the message")]
    public async Task ThenOneManualReportIsShownAsync()
    {
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("manual");
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("ARF email: Not configured");
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("Signed webhook: Not configured");
        await Assertions.Expect(this.Page.Locator("#report-spam")).ToBeDisabledAsync();
    }

    [Given("an active feedback subdomain with an unreachable webhook")]
    public async Task GivenUnreachableFeedbackWebhookAsync()
    {
        await this.PrepareSmtpAsync();
        await this.Page.GotoAsync($"/admin/subdomains/{this.SmtpFixture.SubdomainId}/spam-feedback");
        await this.Page.GetByLabel("Signed webhook URL")
            .FillAsync($"https://feedback.{this.SmtpFixture.DomainName}/spam");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save feedback settings" }).ClickAsync();
        await Assertions.Expect(this.Page.Locator("#webhook-secret")).ToBeVisibleAsync();
    }

    [When("I report a captured message with that webhook")]
    public async Task WhenIReportWithUnreachableWebhookAsync()
    {
        this.feedbackSubject = $"Retry feedback {Guid.NewGuid():N}";
        var response = await this.SmtpFixture.SendAsync(this.SmtpFixture.Recipient, this.feedbackSubject);
        Assert.True(response.Accepted);
        await this.SmtpFixture.WaitForMessageAsync(this.feedbackSubject);
        await this.Page.GotoAsync("/m/inbox");
        await this.Page.ReloadAsync();
        await this.Page.GetByText(this.feedbackSubject, new() { Exact = true }).ClickAsync();
        await this.Page.Locator("#report-spam").ClickAsync();
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToBeVisibleAsync();
    }

    [Then("the webhook failure is visible and I can retry it")]
    public async Task ThenWebhookCanBeRetriedAsync()
    {
        await this.WaitForWebhookAttemptsAsync(1);
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("ARF email: Not configured");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Retry webhook" }).ClickAsync();
        await this.WaitForWebhookAttemptsAsync(2);
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("Spam report recorded (manual)");
    }

    private async Task WaitForWebhookAttemptsAsync(int attempts)
    {
        var timeout = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < timeout)
        {
            await this.Page.GetByRole(AriaRole.Button, new() { Name = "Refresh delivery status" }).ClickAsync();
            var status = await this.Page.Locator("#spam-report-status").InnerTextAsync();
            if (status.Contains("Signed webhook: Failed", StringComparison.Ordinal) &&
                status.Contains($"({attempts} attempts)", StringComparison.Ordinal))
            {
                return;
            }

            await this.Page.WaitForTimeoutAsync(1000);
        }

        throw new TimeoutException($"Webhook did not show failed attempt {attempts}.");
    }

    [Given("an active spam-report chaos address with an ARF destination")]
    public async Task GivenSpamReportChaosAddressAsync()
    {
        await this.PrepareSmtpAsync(chaos: true, reportSpam: true);
        await this.SmtpFixture.ConfigureArfAsync();
    }

    [When("I send campaign mail to the spam-report chaos address")]
    public async Task WhenISendCampaignToSpamChaosAsync()
    {
        this.feedbackCampaign = $"complaint-{Guid.NewGuid():N}";
        this.feedbackSubject = $"Spam feedback {Guid.NewGuid():N}";
        this.feedbackSmtpResult = await this.SmtpFixture.SendAsync(
            this.SmtpFixture.ChaosRecipient!, this.feedbackSubject, this.feedbackCampaign);
    }

    [Then("SMTP accepts it and the campaign is attributed to one spam report")]
    public async Task ThenCampaignSpamReportIsAttributedAsync()
    {
        Assert.True(this.feedbackSmtpResult.Accepted);
        Assert.Equal(250, this.feedbackSmtpResult.Code);
        var message = await this.SmtpFixture.WaitForMessageAsync(this.feedbackSubject);
        this.feedbackEmailId = message.Id;
        this.feedbackReport = await this.SmtpFixture.WaitForReportAsync(message.Id);
        Assert.Equal(SpamReportTrigger.ChaosAddress, this.feedbackReport.Trigger);
        Assert.Equal(this.feedbackCampaign, this.feedbackReport.CampaignValue);
        Assert.Equal(message.CampaignId, this.feedbackReport.CampaignId);
        Assert.NotNull(this.feedbackReport.CampaignId);
        await this.Page.GotoAsync($"/m/campaigns/{this.feedbackReport.CampaignId}");
        await Assertions.Expect(this.Page.GetByText(this.feedbackSubject, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("chaos address");
    }

    [Then("the ARF feedback includes the original message")]
    public async Task ThenArfIncludesOriginalAsync()
    {
        this.arfMessage = await this.SmtpFixture.DomainFixture.WaitForFeedbackMessageAsync();
        var body = Assert.IsType<MultipartReport>(this.arfMessage.Body);
        Assert.Equal("feedback-report", body.ContentType.Parameters["report-type"]);
        var original = Assert.IsType<MessagePart>(body[2]).Message;
        Assert.NotNull(original);
        Assert.Equal(this.feedbackSubject, original.Subject);
        await this.Page.Locator("#spam-report-status").GetByRole(AriaRole.Button, new() { Name = "Refresh delivery status" }).ClickAsync();
        await Assertions.Expect(this.Page.Locator("#spam-report-status")).ToContainTextAsync("ARF email: Delivered");
    }

    [Given("an active failure chaos address for feedback")]
    public Task GivenFailureChaosAddressAsync() => this.PrepareSmtpAsync(chaos: true);

    [When("I send mail to the failure chaos address")]
    public async Task WhenISendToFailureChaosAsync()
    {
        this.feedbackSubject = $"No report {Guid.NewGuid():N}";
        this.feedbackSmtpResult = await this.SmtpFixture.SendAsync(this.SmtpFixture.ChaosRecipient!, this.feedbackSubject);
    }

    [Then("SMTP rejects it without creating a spam report")]
    public async Task ThenFailureChaosHasNoReportAsync()
    {
        Assert.False(this.feedbackSmtpResult.Accepted);
        Assert.Equal(550, this.feedbackSmtpResult.Code);
        Assert.Equal(0, await this.SmtpFixture.CountMessagesAsync(this.feedbackSubject));
        await this.Page.GotoAsync("/chaos-addresses");
        var row = this.Page.Locator("tbody tr").Filter(new() { HasText = this.SmtpFixture.ChaosRecipient });
        await Assertions.Expect(row).ToContainTextAsync("MailboxUnavailablePermanent");
    }
}
