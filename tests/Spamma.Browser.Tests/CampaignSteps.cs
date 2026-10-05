using System.Text.Json;
using BluQube.Constants;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private Guid reviewCampaignId;
    private string campaignValue = string.Empty;
    private string secondSubdomainCampaignValue = string.Empty;
    private IAPIResponse? unauthorizedCampaignResponse;

    private async Task OpenCampaignsAsync()
    {
        await this.Page.GotoAsync("/m/campaigns");
        await this.Page.Locator("#campaign-subdomain").SelectOptionAsync(this.InboxFixture.SubdomainId.ToString());
    }

    [Given("no campaigns are available to me")]
    public Task GivenNoCampaignsAsync() => this.PrepareInboxAsync(restrictedCampaignUser: true);

    [Given("a campaign has been captured for my assigned subdomain")]
    [Given("I am allowed to manage a captured campaign")]
    public Task GivenMyCampaignAsync() => this.PrepareInboxAsync(async fixture =>
    {
        this.campaignValue = "Campaign fixture";
        this.reviewCampaignId = await fixture.SeedCampaignAsync(this.campaignValue);
    }, restrictedCampaignUser: true);

    [Given("several campaigns are available across my assigned subdomains")]
    public Task GivenSeveralCampaignsAsync() => this.PrepareInboxAsync(async fixture =>
    {
        await fixture.SeedCampaignSummariesAsync(52, fixture.SubdomainId);
        this.secondSubdomainCampaignValue = "Second subdomain campaign";
        await fixture.SeedCampaignSummaryAsync(this.secondSubdomainCampaignValue,
            fixture.SubdomainIds[1], DateTimeOffset.UtcNow, 1);
    }, restrictedCampaignUser: true);

    [Given("a campaign belongs to another user's subdomain")]
    public Task GivenOtherUsersCampaignAsync() => this.PrepareInboxAsync(async fixture =>
    {
        this.campaignValue = "Private campaign";
        this.reviewCampaignId = await fixture.SeedOtherUsersCampaignAsync(this.campaignValue);
    }, restrictedCampaignUser: true);

    [Given("I can view but not manage a captured campaign")]
    public Task GivenViewOnlyCampaignAsync() => this.PrepareInboxAsync(async fixture =>
    {
        this.campaignValue = "View-only campaign";
        this.reviewCampaignId = await fixture.SeedCampaignAsync(this.campaignValue);
    }, restrictedCampaignUser: true, viewer: true);

    [When("I open Campaigns")]
    public Task WhenIOpenCampaignsAsync() => this.OpenCampaignsAsync();

    [Then("I see that no campaigns were found")]
    public async Task ThenNoCampaignsFoundAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("campaigns-empty-state")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-row")).ToHaveCountAsync(0);
    }

    [When("I open that campaign")]
    public async Task WhenIOpenThatCampaignAsync()
    {
        await this.OpenCampaignsAsync();
        await this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.campaignValue }).ClickAsync();
    }

    [Then("I can see its campaign details and sample message")]
    public async Task ThenCampaignDetailsAndSampleAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync($"/m/campaigns/{this.reviewCampaignId}");
        await Assertions.Expect(this.Page.GetByTestId("campaign-detail")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.campaignValue, Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Sample for " + this.campaignValue, new() { Exact = true })).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Plain body for Sample for " + this.campaignValue, new() { Exact = false })).ToBeVisibleAsync();
    }

    [When("I filter by the second assigned subdomain")]
    public async Task WhenIFilterSecondSubdomainAsync()
    {
        await this.OpenCampaignsAsync();
        await this.Page.Locator("#campaign-subdomain").SelectOptionAsync(this.InboxFixture.SubdomainIds[1].ToString());
    }

    [Then("the list contains only that subdomain's campaign")]
    public async Task ThenSecondSubdomainCampaignOnlyAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("campaign-row")).ToHaveCountAsync(1);
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").GetByText(this.secondSubdomainCampaignValue)).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Campaign 52", new() { Exact = true })).ToHaveCountAsync(0);
    }

    [When("I select the first subdomain and sort by campaign name")]
    public async Task WhenISortFirstSubdomainAsync()
    {
        await this.Page.Locator("#campaign-subdomain").SelectOptionAsync(this.InboxFixture.SubdomainId.ToString());
        await this.Page.Locator("#campaign-sort").SelectOptionAsync("CampaignValue");
    }

    [Then("campaigns appear in descending name order")]
    public async Task ThenCampaignsSortedAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("campaign-row")).ToHaveCountAsync(50);
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").Nth(0).GetByText("Campaign 52")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").Nth(1).GetByText("Campaign 51")).ToBeVisibleAsync();
    }

    [Then("I can move to the next page of sorted results")]
    public async Task ThenNextCampaignPageAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Next page" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-row")).ToHaveCountAsync(2);
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").Nth(0).GetByText("Campaign 02")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").Nth(1).GetByText("Campaign 01")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#campaign-subdomain")).ToHaveValueAsync(this.InboxFixture.SubdomainId.ToString());
        await Assertions.Expect(this.Page.Locator("#campaign-sort")).ToHaveValueAsync("CampaignValue");
    }

    [When("I delete that campaign")]
    public async Task WhenIDeleteCampaignAsync()
    {
        await this.OpenCampaignsAsync();
        var row = this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.campaignValue });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
    }

    [Then("it no longer appears in the campaign list")]
    public async Task ThenCampaignRemovedAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.campaignValue })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByTestId("campaigns-empty-state")).ToBeVisibleAsync();
    }

    [When("I use the campaign list or a direct campaign link")]
    public async Task WhenIInspectOtherUsersCampaignAsync()
    {
        await this.OpenCampaignsAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.campaignValue })).ToHaveCountAsync(0);
        await this.Page.GotoAsync($"/m/campaigns/{this.reviewCampaignId}");
        this.unauthorizedCampaignResponse = await this.Page.Context.APIRequest.PostAsync(
            $"/api/email-inbox/campaigns/{this.reviewCampaignId}",
            new() { DataObject = new { CampaignId = this.reviewCampaignId } });
    }

    [Then("I cannot view its details or sample message")]
    public async Task ThenOtherUsersCampaignDeniedAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("campaign-detail-empty")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-detail")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText(this.campaignValue, new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText("Sample for " + this.campaignValue, new() { Exact = true })).ToHaveCountAsync(0);
        Assert.NotNull(this.unauthorizedCampaignResponse);
        using var apiResult = JsonDocument.Parse(await this.unauthorizedCampaignResponse.TextAsync());
        Assert.Equal((int)QueryResultStatus.Unauthorized, apiResult.RootElement.GetProperty("Status").GetInt32());
        Assert.False(apiResult.RootElement.TryGetProperty("Data", out var data) && data.ValueKind != JsonValueKind.Null);
    }

    [Then("I can inspect the campaign without a Delete action")]
    public async Task ThenViewerCannotDeleteAsync()
    {
        var row = this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.campaignValue });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await Assertions.Expect(row.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToHaveCountAsync(0);
        await row.ClickAsync();
        await Assertions.Expect(this.Page.GetByTestId("campaign-detail")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Sample for " + this.campaignValue, new() { Exact = true })).ToBeVisibleAsync();
    }
}

