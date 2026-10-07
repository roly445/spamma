using System.Text.RegularExpressions;
using MimeKit;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private SmtpScenarioFixture? smtpFixture;
    private string smtpSubject = string.Empty;
    private string smtpCampaignValue = string.Empty;
    private (bool Accepted, int Code, string Response) smtpResult;
    private (bool Accepted, int Code, string Response)[] concurrentSmtpResults = [];
    private string[] concurrentSubjects = [];
    private string bounceSubject = string.Empty;

    private SmtpScenarioFixture SmtpFixture => this.smtpFixture
        ?? throw new InvalidOperationException("The SMTP fixture has not been created.");

    [AfterScenario("smtp")]
    public async Task DisposeSmtpFixtureAsync()
    {
        if (this.smtpFixture is not null) await this.smtpFixture.DisposeAsync();
    }

    private async Task PrepareSmtpAsync(bool suspendDomain = false, bool suspendSubdomain = false,
        bool chaos = false, bool reportSpam = false)
    {
        this.smtpFixture = await SmtpScenarioFixture.CreateAsync(suspendDomain, suspendSubdomain, chaos, reportSpam);
        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.SmtpFixture.DomainFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.SmtpFixture.DomainFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task ShowSmtpObservationAsync(string detail)
    {
        await this.Page.EvaluateAsync("""
        (detail) => {
            const panel = document.createElement('aside');
            panel.setAttribute('data-testid', 'smtp-test-observation');
            panel.style.cssText = 'position:fixed;bottom:16px;right:16px;z-index:2147483647;max-width:480px;padding:16px;background:#172554;color:white;border:2px solid #60a5fa;border-radius:8px;font:16px/1.4 sans-serif;box-shadow:0 4px 20px #0008';
            const title = document.createElement('strong');
            title.textContent = 'Live SMTP test observation';
            const body = document.createElement('div');
            body.textContent = detail;
            panel.append(title, body);
            document.body.append(panel);
        }
        """, detail);
        await Assertions.Expect(this.Page.GetByTestId("smtp-test-observation")).ToBeVisibleAsync();
    }

    [Given("I am signed in with access to an active verified subdomain")]
    public Task GivenActiveSmtpSubdomainAsync() => this.PrepareSmtpAsync();

    [Given("I am signed in with access to a suspended (.*)")]
    public Task GivenSuspendedSmtpResourceAsync(string resource) => resource switch
    {
        "domain" => this.PrepareSmtpAsync(suspendDomain: true),
        "subdomain" => this.PrepareSmtpAsync(suspendSubdomain: true),
        _ => throw new ArgumentOutOfRangeException(nameof(resource)),
    };

    [Given("I am signed in with access to an active chaos address")]
    public Task GivenActiveSmtpChaosAddressAsync() => this.PrepareSmtpAsync(chaos: true);

    [When("I retry the same campaign message twice against a temporary chaos address")]
    public async Task WhenIRetryTemporaryCampaignMessageAsync()
    {
        this.smtpCampaignValue = $"SMTP bounce {Guid.NewGuid():N}";
        this.bounceSubject = $"Retry {Guid.NewGuid():N}";
        var message = new MimeMessage
        {
            From = { MailboxAddress.Parse("sender@external.example") },
            To = { MailboxAddress.Parse(this.SmtpFixture.TemporaryChaosRecipient!) },
            Subject = this.bounceSubject,
            Body = new TextPart("plain") { Text = "The same message is sent twice." },
        };
        message.Headers.Add("X-Spamma-Camp", this.smtpCampaignValue);
        var first = await this.SmtpFixture.SendMessageAsync(message);
        var second = await this.SmtpFixture.SendMessageAsync(message);
        Assert.Equal(450, first.Code);
        Assert.Equal(450, second.Code);
    }

    [When("I send a campaign message to a permanent chaos address")]
    public async Task WhenISendPermanentCampaignMessageAsync()
    {
        this.smtpCampaignValue = $"SMTP hard bounce {Guid.NewGuid():N}";
        this.bounceSubject = $"Rejected {Guid.NewGuid():N}";
        this.smtpResult = await this.SmtpFixture.SendAsync(this.SmtpFixture.ChaosRecipient!, this.bounceSubject, this.smtpCampaignValue);
        Assert.Equal(550, this.smtpResult.Code);
    }

    [When("a temporary campaign failure is followed by accepted mail")]
    public async Task WhenBounceFollowedByAcceptedMailAsync()
    {
        this.smtpCampaignValue = $"SMTP recovered {Guid.NewGuid():N}";
        this.bounceSubject = $"Initially rejected {Guid.NewGuid():N}";
        var rejected = await this.SmtpFixture.SendAsync(this.SmtpFixture.TemporaryChaosRecipient!, this.bounceSubject, this.smtpCampaignValue);
        Assert.Equal(450, rejected.Code);
        await this.SmtpFixture.WaitForCampaignCountsAsync(this.smtpCampaignValue, 0, 1, 0, 1);
        this.smtpSubject = $"Accepted after bounce {Guid.NewGuid():N}";
        var accepted = await this.SmtpFixture.SendAsync(this.SmtpFixture.Recipient, this.smtpSubject, this.smtpCampaignValue);
        Assert.True(accepted.Accepted);
    }

    [Then("the campaign shows one temporary failure and two SMTP attempts without an inbox message")]
    public Task ThenTemporaryRetryCountsAsync() => this.ShowBounceCampaignAsync(0, 1, 0, 2, false);

    [Then("the campaign shows one permanent rejection without an inbox message")]
    public Task ThenPermanentRejectionCountsAsync() => this.ShowBounceCampaignAsync(0, 0, 1, 1, false);

    [Then("the campaign shows the failure separately from its captured sample")]
    public Task ThenRecoveredCampaignCountsAsync() => this.ShowBounceCampaignAsync(1, 1, 0, 1, true);

    private async Task ShowBounceCampaignAsync(int captured, int temporary, int permanent, int attempts, bool hasSample)
    {
        var campaign = await this.SmtpFixture.WaitForCampaignCountsAsync(
            this.smtpCampaignValue, captured, temporary, permanent, attempts);
        Assert.Equal(0, await this.SmtpFixture.CountMessagesAsync(this.bounceSubject));
        await this.Page.GotoAsync("/m/campaigns");
        await this.Page.Locator("#campaign-subdomain").SelectOptionAsync(this.SmtpFixture.SubdomainId.ToString());
        var row = this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.smtpCampaignValue });
        await Assertions.Expect(row).ToContainTextAsync($"{temporary} temporary failures");
        await Assertions.Expect(row).ToContainTextAsync($"{permanent} permanent rejections");
        await Assertions.Expect(row).ToContainTextAsync($"{attempts} failed SMTP attempts");
        await row.ClickAsync();
        var detail = this.Page.GetByTestId("campaign-detail");
        await Assertions.Expect(detail.Locator("div.rounded-md").Filter(new() { HasText = "Temporary SMTP failures" })
            .GetByText(temporary.ToString(), new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(detail.Locator("div.rounded-md").Filter(new() { HasText = "Permanent SMTP rejections" })
            .GetByText(permanent.ToString(), new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(detail.Locator("div.rounded-md").Filter(new() { HasText = "Failed SMTP attempts" })
            .GetByText(attempts.ToString(), new() { Exact = true })).ToBeVisibleAsync();
        if (hasSample)
        {
            Assert.NotNull(campaign.SampleMessageId);
            await Assertions.Expect(detail.GetByText(this.smtpSubject, new() { Exact = true })).ToBeVisibleAsync();
        }
        else
        {
            Assert.Null(campaign.SampleMessageId);
            await Assertions.Expect(detail.GetByText("No sample message available.")).ToBeVisibleAsync();
        }

        await this.ShowSmtpObservationAsync($"Campaign: {captured} captured, {temporary} temporary failures, {permanent} permanent rejections, {attempts} failed SMTP attempts.");
    }

    [When("I deliver a standard message to that subdomain over SMTP")]
    public async Task WhenIDeliverStandardSmtpMessageAsync()
    {
        this.smtpSubject = $"SMTP delivered {Guid.NewGuid():N}";
        this.smtpResult = await this.SmtpFixture.SendAsync(this.SmtpFixture.Recipient, this.smtpSubject);
    }

    [When("I deliver a message to an unconfigured domain over SMTP")]
    public async Task WhenIDeliverToUnknownDomainAsync()
    {
        this.smtpSubject = $"SMTP unknown {Guid.NewGuid():N}";
        this.smtpResult = await this.SmtpFixture.SendAsync("receiver@unconfigured.invalid", this.smtpSubject);
    }

    [When("I deliver a message to that subdomain over SMTP")]
    public async Task WhenIDeliverToSuspendedSubdomainAsync()
    {
        this.smtpSubject = $"SMTP suspended {Guid.NewGuid():N}";
        this.smtpResult = await this.SmtpFixture.SendAsync(this.SmtpFixture.Recipient, this.smtpSubject);
    }

    [When("I deliver a message with a unique campaign identifier over SMTP")]
    public async Task WhenIDeliverCampaignSmtpMessageAsync()
    {
        this.smtpCampaignValue = $"SMTP campaign {Guid.NewGuid():N}";
        this.smtpSubject = $"Sample for {this.smtpCampaignValue}";
        this.smtpResult = await this.SmtpFixture.SendAsync(this.SmtpFixture.Recipient,
            this.smtpSubject, this.smtpCampaignValue);
    }

    [When("I deliver a message to the chaos address over SMTP")]
    public async Task WhenIDeliverToChaosAddressAsync()
    {
        this.smtpSubject = $"SMTP chaos {Guid.NewGuid():N}";
        this.smtpResult = await this.SmtpFixture.SendAsync(this.SmtpFixture.ChaosRecipient!, this.smtpSubject);
    }

    [When("five senders deliver distinct messages concurrently over SMTP")]
    public async Task WhenFiveMessagesArriveConcurrentlyAsync()
    {
        this.concurrentSubjects = Enumerable.Range(1, 5)
            .Select(index => $"SMTP concurrent {index} {Guid.NewGuid():N}").ToArray();
        this.concurrentSmtpResults = await Task.WhenAll(this.concurrentSubjects.Select((subject, index) =>
            this.SmtpFixture.SendAsync(this.SmtpFixture.Recipient, subject,
                sender: $"sender{index + 1}@external.example")));
    }

    [Then("SMTP accepts the message")]
    public async Task ThenSmtpAcceptsMessageAsync()
    {
        Assert.True(this.smtpResult.Accepted, $"SMTP {this.smtpResult.Code}: {this.smtpResult.Response}");
        Assert.Equal(250, this.smtpResult.Code);
        await this.ShowSmtpObservationAsync($"250 accepted: {this.smtpSubject}");
    }

    [Then("SMTP rejects the message with mailbox name not allowed")]
    public async Task ThenSmtpRejectsUnknownMailboxAsync()
    {
        Assert.False(this.smtpResult.Accepted);
        Assert.Equal(553, this.smtpResult.Code);
        await this.ShowSmtpObservationAsync($"{this.smtpResult.Code} rejected: {this.smtpSubject}");
    }

    [Then("SMTP accepts every message")]
    public async Task ThenSmtpAcceptsAllMessagesAsync()
    {
        Assert.Equal(5, this.concurrentSmtpResults.Length);
        Assert.All(this.concurrentSmtpResults, result =>
        {
            Assert.True(result.Accepted, $"SMTP {result.Code}: {result.Response}");
            Assert.Equal(250, result.Code);
        });
        await this.ShowSmtpObservationAsync("Five distinct concurrent messages: all returned SMTP 250.");
    }

    [Then("SMTP returns the configured failure code")]
    public async Task ThenSmtpReturnsChaosFailureAsync()
    {
        Assert.False(this.smtpResult.Accepted);
        Assert.Equal(550, this.smtpResult.Code);
        await this.ShowSmtpObservationAsync($"Configured chaos response: {this.smtpResult.Code} {this.smtpResult.Response}");
    }

    [Then("the sender can see the rejected response")]
    public async Task ThenSenderSeesChaosFailureAsync()
    {
        Assert.Contains("550", $"{this.smtpResult.Code} {this.smtpResult.Response}");
        Assert.Equal(0, await this.SmtpFixture.CountMessagesAsync(this.smtpSubject));
        await Assertions.Expect(this.Page.GetByTestId("smtp-test-observation")).ToContainTextAsync("550");
    }

    [Then("I can inspect the delivered message in my inbox")]
    public async Task ThenIInspectDeliveredSmtpMessageAsync()
    {
        await this.SmtpFixture.WaitForMessageAsync(this.smtpSubject);
        Assert.Equal(1, await this.SmtpFixture.CountMessagesAsync(this.smtpSubject));
        await this.Page.ReloadAsync();
        await Assertions.Expect(this.Page.GetByText(this.smtpSubject, new() { Exact = true })).ToBeVisibleAsync();
        await this.Page.GetByText(this.smtpSubject, new() { Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.smtpSubject })).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText($"SMTP body for {this.smtpSubject}", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Then("the rejected message is absent from my inbox")]
    public async Task ThenRejectedSmtpMessageIsAbsentAsync()
    {
        Assert.Equal(0, await this.SmtpFixture.CountMessagesAsync(this.smtpSubject));
        await this.Page.ReloadAsync();
        await Assertions.Expect(this.Page.GetByText(this.smtpSubject, new() { Exact = true })).ToHaveCountAsync(0);
        await this.ShowSmtpObservationAsync($"SMTP {this.smtpResult.Code}; no stored or visible message for {this.smtpSubject}");
    }

    [Then("each message appears exactly once in my inbox")]
    public async Task ThenEachSmtpMessageAppearsOnceAsync()
    {
        foreach (var subject in this.concurrentSubjects)
        {
            await this.SmtpFixture.WaitForMessageAsync(subject);
            Assert.Equal(1, await this.SmtpFixture.CountMessagesAsync(subject));
        }
        await this.Page.ReloadAsync();
        foreach (var subject in this.concurrentSubjects)
            await Assertions.Expect(this.Page.GetByText(subject, new() { Exact = true })).ToHaveCountAsync(1);
        await this.ShowSmtpObservationAsync("Five SMTP 250 responses; five distinct inbox messages, one copy each.");
    }

    [Then("the campaign shows one capture and its sample content")]
    public async Task ThenCampaignHasOneCaptureAndSampleAsync()
    {
        var campaign = await this.SmtpFixture.WaitForCampaignAsync(this.smtpCampaignValue);
        Assert.Equal(1, campaign.TotalCaptured);
        Assert.NotNull(campaign.SampleMessageId);
        Assert.Equal(1, await this.SmtpFixture.CountMessagesAsync(this.smtpSubject));
        await this.Page.GotoAsync("/m/campaigns");
        await this.Page.Locator("#campaign-subdomain").SelectOptionAsync(this.SmtpFixture.SubdomainId.ToString());
        var row = this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.smtpCampaignValue });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await row.ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading,
            new() { Name = this.smtpCampaignValue, Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.smtpSubject, new() { Exact = true })).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText($"SMTP body for {this.smtpSubject}",
            new() { Exact = false })).ToBeVisibleAsync();
        await this.ShowSmtpObservationAsync("One campaign capture and one stored sample from live SMTP delivery.");
    }
}
