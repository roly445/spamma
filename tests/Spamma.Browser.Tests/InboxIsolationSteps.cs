using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private InboxScenarioFixture.SecondUserBoundary? secondUserBoundary;
    private string firstUserSubject = string.Empty;
    private string secondUserSubject = string.Empty;
    private string firstUserCampaign = string.Empty;
    private Guid firstUserMessageId;
    private Guid firstUserCampaignId;

    private InboxScenarioFixture.SecondUserBoundary SecondUserBoundary => this.secondUserBoundary
        ?? throw new InvalidOperationException("The second inbox user has not been seeded.");

    private async Task SignInAsInboxUserAsync(string emailAddress)
    {
        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(emailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        var loginPath = await this.InboxFixture.WaitForLoginPathAsync();
        await this.Page.GotoAsync(loginPath);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task AssertVisibleDomainAsync(string visibleName, string hiddenName)
    {
        await this.Page.GotoAsync("/admin/domains");
        await Assertions.Expect(this.Page.GetByText(visibleName, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(hiddenName, new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Given("two users have separate domains, messages, and a captured campaign")]
    public async Task GivenTwoUserInboxBoundaryAsync()
    {
        this.inboxFixture = await InboxScenarioFixture.CreateAsync(restrictedCampaignUser: true, viewer: true);
        this.secondUserBoundary = await this.InboxFixture.SeedSecondUserBoundaryAsync();
        this.firstUserSubject = $"First inbox {Guid.NewGuid():N}";
        this.secondUserSubject = $"Second inbox {Guid.NewGuid():N}";
        this.firstUserCampaign = $"First campaign {Guid.NewGuid():N}";
        this.firstUserMessageId = await this.InboxFixture.SeedMessageAsync(this.firstUserSubject,
            subdomainId: this.SecondUserBoundary.FirstSubdomainId);
        await this.InboxFixture.SeedMessageAsync(this.secondUserSubject,
            subdomainId: this.SecondUserBoundary.SecondSubdomainId,
            domainIdOverride: this.SecondUserBoundary.SecondDomainId);
        this.firstUserCampaignId = await this.InboxFixture.SeedCampaignAsync(this.firstUserCampaign,
            this.SecondUserBoundary.FirstSubdomainId);
    }

    [When("the first user signs in using a magic link")]
    public Task WhenFirstInboxUserSignsInAsync() => this.SignInAsInboxUserAsync(this.InboxFixture.EmailAddress);

    [Then("the first user can inspect their message and campaign")]
    public async Task ThenFirstInboxUserSeesOwnContentAsync()
    {
        await Assertions.Expect(this.Page.GetByText(this.firstUserSubject, new() { Exact = true })).ToBeVisibleAsync();
        await this.SearchAsync(this.firstUserSubject);
        await this.Page.GetByText(this.firstUserSubject, new() { Exact = true }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Plain body for " + this.firstUserSubject)).ToBeVisibleAsync();

        await this.Page.GotoAsync("/m/campaigns");
        await this.Page.Locator("#campaign-subdomain").SelectOptionAsync(this.SecondUserBoundary.FirstSubdomainId.ToString());
        await this.Page.GetByTestId("campaign-row").Filter(new() { HasText = this.firstUserCampaign }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync($"/m/campaigns/{this.firstUserCampaignId}");
        await Assertions.Expect(this.Page.GetByText("Sample for " + this.firstUserCampaign,
            new() { Exact = true })).ToBeVisibleAsync();
    }

    [Then("the second user's domain and message are hidden")]
    public async Task ThenSecondUserContentHiddenFromFirstAsync()
    {
        await this.Page.GotoAsync("/m/inbox");
        await this.SearchAsync(this.secondUserSubject);
        await Assertions.Expect(this.Page.GetByText(this.secondUserSubject, new() { Exact = true })).ToHaveCountAsync(0);
        await this.AssertVisibleDomainAsync(this.SecondUserBoundary.FirstDomainName,
            this.SecondUserBoundary.SecondDomainName);
    }

    [When("the second user signs in using a magic link")]
    public async Task WhenSecondInboxUserSignsInAsync()
    {
        await this.Page.GotoAsync("/logout");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading,
            new() { Name = "You've been logged out" })).ToBeVisibleAsync();
        await this.SignInAsInboxUserAsync(this.SecondUserBoundary.EmailAddress);
    }

    [Then("the second user can inspect their own message")]
    public async Task ThenSecondInboxUserSeesOwnMessageAsync()
    {
        await Assertions.Expect(this.Page.GetByText(this.secondUserSubject, new() { Exact = true })).ToBeVisibleAsync();
        await this.Page.GetByText(this.secondUserSubject, new() { Exact = true }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Plain body for " + this.secondUserSubject)).ToBeVisibleAsync();
    }

    [Then("the first user's domain and message are hidden")]
    public async Task ThenFirstUserContentHiddenFromSecondAsync()
    {
        await this.Page.GotoAsync("/m/inbox");
        await this.SearchAsync(this.firstUserSubject);
        await Assertions.Expect(this.Page.GetByText(this.firstUserSubject, new() { Exact = true })).ToHaveCountAsync(0);
        var response = await this.Page.Context.APIRequest.GetAsync(
            $"/api/email-inbox/emails/{this.firstUserMessageId}/mime-content");
        Assert.Equal(401, response.Status);
        await this.AssertVisibleDomainAsync(this.SecondUserBoundary.SecondDomainName,
            this.SecondUserBoundary.FirstDomainName);
    }
}
