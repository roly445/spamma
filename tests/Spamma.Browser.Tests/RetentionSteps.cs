using Microsoft.Playwright;
using Reqnroll;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    [Given("I administer a verified domain with a subdomain for retention")]
    public async Task GivenRetentionDomainAsync()
    {
        await this.PrepareDomainAsync(verified: true);
        this.submissionSubdomainId = await this.DomainFixture.SeedSubdomainAsync(this.reviewDomainId, "team");
    }

    [When("I open the domain retention settings")]
    public Task WhenIOpenDomainRetentionAsync() =>
        this.Page.GotoAsync($"/admin/domains/{this.reviewDomainId}/retention");

    [Then("I see the default 24-hour age limit and no count limit")]
    public async Task ThenDefaultRetentionAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.reviewDomainName })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("24 hours");
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("count: disabled");
    }

    [When("I set a 48-hour and 10-message domain retention policy")]
    public async Task WhenISetDomainRetentionAsync()
    {
        await this.Page.Locator("#retention-age").FillAsync("48");
        await this.Page.GetByLabel("Limit by message count").CheckAsync();
        await this.Page.Locator("#retention-count").FillAsync("10");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save retention settings" }).ClickAsync();
    }

    [Then("the new domain retention policy is active")]
    public async Task ThenDomainRetentionActiveAsync()
    {
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("48 hours");
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("count: 10");
        await Assertions.Expect(this.Page.GetByText("Retention settings saved.")).ToBeVisibleAsync();
        var screenshotDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_SCREENSHOT_DIR");
        if (!string.IsNullOrWhiteSpace(screenshotDirectory))
        {
            Directory.CreateDirectory(screenshotDirectory);
            await this.Page.SetViewportSizeAsync(1280, 900);
            await Assertions.Expect(this.Page.Locator("#blazor-wasm-loading")).ToBeHiddenAsync();
            await this.Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(screenshotDirectory, "email-retention-policy.png"),
                FullPage = true,
            });
        }
    }

    [When("I open retention settings for the subdomain")]
    public Task WhenIOpenSubdomainRetentionAsync() =>
        this.Page.GotoAsync($"/admin/subdomains/{this.submissionSubdomainId}/retention");

    [Then("it inherits the named domain retention policy")]
    public async Task ThenSubdomainInheritsRetentionAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = $"team.{this.reviewDomainName}" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText($"Parent domain: {this.reviewDomainName}")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#retention-mode")).ToHaveValueAsync("Inherit");
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("48 hours");
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("count: 10");
    }

    [When("I override the subdomain to keep 3 messages without an age limit")]
    public async Task WhenIOverrideSubdomainRetentionAsync()
    {
        await this.Page.Locator("#retention-mode").SelectOptionAsync("Override");
        await this.Page.GetByLabel("Limit by age").UncheckAsync();
        await this.Page.Locator("#retention-count").FillAsync("3");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save retention settings" }).ClickAsync();
    }

    [Then("the subdomain retention override is active")]
    public async Task ThenSubdomainRetentionActiveAsync()
    {
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("Age: disabled");
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("count: 3");
    }

    [When("I restore inheritance for the subdomain")]
    public async Task WhenIRestoreSubdomainRetentionAsync()
    {
        await this.Page.Locator("#retention-mode").SelectOptionAsync("Inherit");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save retention settings" }).ClickAsync();
    }

    [Then("the subdomain uses the domain retention policy again")]
    public async Task ThenSubdomainRetentionInheritedAgainAsync()
    {
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("48 hours");
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToContainTextAsync("count: 10");
    }

    [Given("I am signed in without management rights to a retention domain")]
    public Task GivenUnrelatedRetentionDomainAsync() => this.PrepareDomainAsync(administrator: false);

    [When("I open its retention URL directly")]
    public Task WhenIOpenUnrelatedRetentionAsync() =>
        this.Page.GotoAsync($"/admin/domains/{this.reviewDomainId}/retention");

    [Then("the retention policy and domain name are not disclosed")]
    public async Task ThenRetentionNotDisclosedAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("do not have permission");
        await Assertions.Expect(this.Page.GetByText(this.reviewDomainName)).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.Locator("#retention-effective-policy")).ToHaveCountAsync(0);
    }
}
