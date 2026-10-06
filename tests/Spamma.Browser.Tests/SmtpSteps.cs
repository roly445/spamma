using System.Text.RegularExpressions;
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
