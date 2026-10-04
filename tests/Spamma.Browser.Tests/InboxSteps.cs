using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private InboxScenarioFixture? inboxFixture;
    private Guid inboxMessageId;
    private string inboxSubject = string.Empty;
    private string unrelatedSubject = string.Empty;
    private Guid otherUserMessageId;
    private Guid campaignId;
    private IDownload? download;
    private string? printableContent;

    private InboxScenarioFixture InboxFixture => this.inboxFixture
        ?? throw new InvalidOperationException("The inbox fixture has not been created.");

    [AfterScenario("inbox")]
    public async Task DisposeInboxFixtureAsync()
    {
        if (this.inboxFixture is not null)
        {
            await this.inboxFixture.DisposeAsync();
        }
    }

    private async Task PrepareInboxAsync(Func<InboxScenarioFixture, Task>? seed = null)
    {
        this.inboxFixture = await InboxScenarioFixture.CreateAsync();
        if (seed is not null)
        {
            await seed(this.inboxFixture);
        }

        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.inboxFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        var loginPath = await this.inboxFixture.WaitForLoginPathAsync();
        await this.Page.GotoAsync(loginPath);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task SeedOneMessageAsync(InboxScenarioFixture fixture, bool attachment = false)
    {
        this.inboxSubject = $"Inbox fixture {Guid.NewGuid():N}";
        this.inboxMessageId = await fixture.SeedMessageAsync(this.inboxSubject, includeAttachment: attachment);
    }

    private async Task OpenFixtureMessageAsync()
    {
        await this.Page.GetByText(this.inboxSubject, new() { Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.inboxSubject })).ToBeVisibleAsync();
    }

    private async Task SearchAsync(string term)
    {
        await this.Page.GetByPlaceholder("Search emails...").FillAsync(term);
        await this.Page.GetByPlaceholder("Search emails...").PressAsync("Enter");
    }

    [Given("I am signed in with access to a subdomain without messages")]
    public Task GivenIHaveAnEmptyInboxAsync() => this.PrepareInboxAsync();

    [Given("a non-campaign message has been received for a subdomain I can view")]
    public Task GivenAReceivedMessageAsync() => this.PrepareInboxAsync(f => this.SeedOneMessageAsync(f));

    [Given("my inbox contains messages with different subjects and email addresses")]
    public Task GivenDifferentMessagesAsync() => this.PrepareInboxAsync(async fixture =>
    {
        this.inboxSubject = "Needle subject " + Guid.NewGuid().ToString("N");
        this.unrelatedSubject = "Other subject " + Guid.NewGuid().ToString("N");
        this.inboxMessageId = await fixture.SeedMessageAsync(this.inboxSubject, "needle-sender@example.test");
        await fixture.SeedMessageAsync(this.unrelatedSubject, "other-sender@example.test");
    });

    [Given("my inbox contains messages")]
    public Task GivenInboxContainsMessagesAsync() => this.PrepareInboxAsync(f => this.SeedOneMessageAsync(f));

    [Given("my search has more than one page of results")]
    public async Task GivenPagedSearchResultsAsync()
    {
        await this.PrepareInboxAsync(async fixture =>
        {
            for (var i = 0; i < 27; i++)
            {
                await fixture.SeedMessageAsync($"Page match {i:D2}", receivedAt: DateTimeOffset.UtcNow.AddMinutes(-i));
            }
        });

        await this.SearchAsync("Page match");
        await Assertions.Expect(this.Page.GetByText("Page 1 of 2", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Given("I have opened a non-campaign message in my inbox")]
    [Given("I have opened a message in my inbox")]
    [Given("I have opened a message with HTML and plain text content")]
    public async Task GivenIOpenedAMessageAsync()
    {
        await this.PrepareInboxAsync(f => this.SeedOneMessageAsync(f));
        await this.OpenFixtureMessageAsync();
    }

    [Given("I have opened a message with an attachment")]
    public async Task GivenIOpenedAnAttachmentAsync()
    {
        await this.PrepareInboxAsync(f => this.SeedOneMessageAsync(f, attachment: true));
        await this.OpenFixtureMessageAsync();
    }

    [Given("a campaign message has been received for a subdomain I can view")]
    public Task GivenACampaignMessageAsync() => this.PrepareInboxAsync(async fixture =>
    {
        this.inboxSubject = "Campaign fixture " + Guid.NewGuid().ToString("N");
        this.campaignId = Guid.NewGuid();
        this.inboxMessageId = await fixture.SeedMessageAsync(this.inboxSubject, campaignId: this.campaignId);
    });

    [Given("another user has a message outside my assignments")]
    public Task GivenOtherUsersMessageAsync() => this.PrepareInboxAsync(async fixture =>
    {
        this.inboxSubject = "Private fixture " + Guid.NewGuid().ToString("N");
        this.otherUserMessageId = await fixture.SeedOtherUsersMessageAsync(this.inboxSubject);
    });

    [When("I open my inbox")]
    public Task WhenIOpenInboxAsync() => this.Page.GotoAsync("/m/inbox");

    [Then("I see the empty inbox state")]
    public Task ThenISeeEmptyInboxAsync() =>
        Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "No emails" })).ToBeVisibleAsync();

    [Then("I can see that message in the list")]
    public Task ThenISeeMessageInListAsync() =>
        Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToBeVisibleAsync();

    [Then("I can select it to inspect its sender, recipients, subject, content, and headers")]
    public async Task ThenICanInspectMessageAsync()
    {
        await this.OpenFixtureMessageAsync();
        await Assertions.Expect(this.Page.GetByText("sender@example.test", new() { Exact = false })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("recipient@example.test", new() { Exact = false }).Last).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Raw" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Subject: " + this.inboxSubject, new() { Exact = false })).ToBeVisibleAsync();
    }

    [When("I search by part of a subject or email address")]
    public async Task WhenISearchForSubjectOrAddressAsync()
    {
        await this.SearchAsync("Needle subject");
        await Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToBeVisibleAsync();
        await this.SearchAsync("needle-sender@example.test");
        await Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToBeVisibleAsync();
    }

    [Then("matching messages are shown")]
    public Task ThenMatchingMessagesShownAsync() =>
        Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToBeVisibleAsync();

    [Then("unrelated messages are not shown")]
    public Task ThenUnrelatedMessagesHiddenAsync() =>
        Assertions.Expect(this.Page.GetByText(this.unrelatedSubject, new() { Exact = true })).ToHaveCountAsync(0);

    [When("I search for a term that matches none of them")]
    public Task WhenISearchWithoutMatchesAsync() => this.SearchAsync("no-matches-" + Guid.NewGuid().ToString("N"));

    [Then("I see the no-emails-found state")]
    public async Task ThenISeeNoSearchResultsAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "No emails found" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Then("I can change my search")]
    public async Task ThenICanChangeMySearchAsync()
    {
        var revisedTerm = "still-no-matches-" + Guid.NewGuid().ToString("N");
        await this.SearchAsync(revisedTerm);
        await Assertions.Expect(this.Page.GetByPlaceholder("Search emails...")).ToHaveValueAsync(revisedTerm);
        await this.ThenISeeNoSearchResultsAsync();
    }

    [When("I move to the next page")]
    public Task WhenIMoveToNextPageAsync() => this.Page.GetByRole(AriaRole.Button, new() { Name = "Next page" }).ClickAsync();

    [Then("I see the next matching messages")]
    public async Task ThenISeeNextPageAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Page 2 of 2", new() { Exact = false })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Page match 26", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Then("the search remains applied")]
    public Task ThenSearchRemainsAppliedAsync() =>
        Assertions.Expect(this.Page.GetByPlaceholder("Search emails...")).ToHaveValueAsync("Page match");

    [When("I mark it as a favorite")]
    public Task WhenIMarkFavoriteAsync() => this.Page.Locator("button[title='Add to favorites']").ClickAsync();

    [Then("the message shows a filled favorite star in the viewer and inbox list")]
    public async Task ThenFavoriteIsShownAsync()
    {
        await Assertions.Expect(this.Page.Locator("button[title='Remove from favorites']")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("svg.text-yellow-500")).ToHaveCountAsync(1);
    }

    [When("I remove it from favorites")]
    public Task WhenIRemoveFavoriteAsync() => this.Page.Locator("button[title='Remove from favorites']").ClickAsync();

    [Then("the favorite star is cleared in the viewer and inbox list")]
    public async Task ThenFavoriteClearedAsync()
    {
        await Assertions.Expect(this.Page.Locator("button[title='Add to favorites']")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("svg.text-yellow-500")).ToHaveCountAsync(0);
    }

    [When("I switch between the HTML, Text, and Raw tabs")]
    public async Task WhenISwitchRepresentationsAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "HTML", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.FrameLocator("iframe").GetByText("HTML body for " + this.inboxSubject)).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Plain body for " + this.inboxSubject)).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Raw", Exact = true }).ClickAsync();
    }

    [Then("I can inspect the corresponding content of that message")]
    public Task ThenRawContentVisibleAsync() =>
        Assertions.Expect(this.Page.GetByText("Subject: " + this.inboxSubject, new() { Exact = false })).ToBeVisibleAsync();

    [When("I save it as {word}")]
    public async Task WhenISaveAsFormatAsync(string format)
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Message" }).ClickAsync();
        this.download = await this.Page.RunAndWaitForDownloadAsync(() =>
            this.Page.GetByRole(AriaRole.Button, new() { Name = "Save as " + format }).ClickAsync());
    }

    [Then("I receive a file with the {word} extension containing that message")]
    public async Task ThenIReceiveMessageFileAsync(string extension)
    {
        Assert.NotNull(this.download);
        Assert.EndsWith(extension, this.download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);
        var path = await this.download.PathAsync();
        Assert.Contains(this.inboxSubject, await File.ReadAllTextAsync(path), StringComparison.Ordinal);
    }

    [When("I choose Save as PDF")]
    public async Task WhenIChoosePdfAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Message" }).ClickAsync();
        var popup = await this.Page.RunAndWaitForPopupAsync(() =>
            this.Page.GetByRole(AriaRole.Button, new() { Name = "Save as PDF" }).ClickAsync());
        this.printableContent = await popup.ContentAsync();
    }

    [Then("the browser opens the message for printing")]
    public void ThenPrintableMessageOpened()
    {
        Assert.NotNull(this.printableContent);
        Assert.Contains(this.inboxSubject, this.printableContent, StringComparison.Ordinal);
        Assert.Contains("HTML body for", this.printableContent, StringComparison.Ordinal);
    }

    [When("I download the attachment")]
    public async Task WhenIDownloadAttachmentAsync() =>
        this.download = await this.Page.RunAndWaitForDownloadAsync(() =>
            this.Page.GetByRole(AriaRole.Button, new() { Name = "Download" }).ClickAsync());

    [Then("I receive the attachment with its original filename and content")]
    public async Task ThenAttachmentDownloadedAsync()
    {
        Assert.NotNull(this.download);
        Assert.Equal("evidence.txt", this.download.SuggestedFilename);
        Assert.Equal("fixture attachment contents", await File.ReadAllTextAsync(await this.download.PathAsync()));
    }

    [When("I delete it")]
    public Task WhenIDeleteMessageAsync() =>
        this.Page.Locator("button[class*='hover:text-red-600']").ClickAsync();

    [Then("it is removed from my inbox")]
    public Task ThenMessageRemovedAsync() =>
        Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToHaveCountAsync(0);

    [Then("I cannot open its content again")]
    public async Task ThenDeletedContentDeniedAsync()
    {
        var response = await this.Page.Context.APIRequest.GetAsync($"/api/email-inbox/emails/{this.inboxMessageId}/mime-content");
        Assert.Equal(401, response.Status);
    }

    [Then("I do not see that message in the list")]
    public Task ThenCampaignHiddenAsync() =>
        Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToHaveCountAsync(0);

    [When("I enable Show campaign emails")]
    public Task WhenIEnableCampaignEmailsAsync() => this.Page.GetByLabel("Show campaign emails").CheckAsync();

    [Then("I can open the campaign message")]
    public Task ThenICanOpenCampaignMessageAsync() => this.OpenFixtureMessageAsync();

    [When("I follow its campaign link")]
    public async Task WhenIFollowCampaignLinkAsync()
    {
        var link = this.Page.GetByRole(AriaRole.Link, new() { Name = "View Campaign" });
        await Assertions.Expect(link).ToHaveAttributeAsync("href", $"/m/campaigns/{this.campaignId}");
        await link.ClickAsync();
    }

    [Then("I see its campaign details")]
    public Task ThenISeeCampaignDetailsAsync() =>
        Assertions.Expect(this.Page).ToHaveURLAsync(new Regex($@"/m/campaigns/{this.campaignId}$"));

    [Then("I cannot favorite or delete that message")]
    public async Task ThenCampaignIsReadOnlyAsync()
    {
        await Assertions.Expect(this.Page.Locator("button[title='Add to favorites']")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.Locator("button[class*='hover:text-red-600']")).ToHaveCountAsync(0);
    }

    [When("I search for that message")]
    public Task WhenISearchForOtherUsersMessageAsync() => this.SearchAsync(this.inboxSubject);

    [Then("it does not appear in my inbox")]
    public Task ThenOtherUsersMessageHiddenAsync() =>
        Assertions.Expect(this.Page.GetByText(this.inboxSubject, new() { Exact = true })).ToHaveCountAsync(0);

    [When("I request its content by message ID")]
    public async Task WhenIRequestOtherUsersContentAsync() =>
        this.otherUserContentResponse = await this.Page.Context.APIRequest.GetAsync(
            $"/api/email-inbox/emails/{this.otherUserMessageId}/mime-content");

    private IAPIResponse? otherUserContentResponse;

    [Then("the request is denied without disclosing the message")]
    public void ThenOtherUsersContentDenied()
    {
        Assert.NotNull(this.otherUserContentResponse);
        Assert.Equal(401, this.otherUserContentResponse.Status);
    }
}
