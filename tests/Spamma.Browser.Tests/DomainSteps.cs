using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? domainFixture;
    private Guid reviewDomainId;
    private string reviewDomainName = string.Empty;
    private string verificationToken = string.Empty;
    private string moderatorEmail = string.Empty;
    private string moderatorName = string.Empty;

    private DomainScenarioFixture DomainFixture => this.domainFixture
        ?? throw new InvalidOperationException("The domain fixture has not been created.");

    [AfterScenario("domain")]
    public async Task DisposeDomainFixtureAsync()
    {
        if (this.domainFixture is not null) await this.domainFixture.DisposeAsync();
    }

    private async Task PrepareDomainAsync(bool administrator = true, bool assigned = false,
        bool verified = false, bool suspended = false, bool seed = true)
    {
        this.domainFixture = await DomainScenarioFixture.CreateAsync(administrator);
        if (seed)
        {
            this.reviewDomainName = $"review-{Guid.NewGuid():N}.example.test";
            (this.reviewDomainId, _, this.verificationToken) = await this.DomainFixture.SeedDomainAsync(
                this.reviewDomainName, verified, suspended);
            if (assigned) await this.DomainFixture.AssignCurrentUserAsync(this.reviewDomainId);
        }

        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.DomainFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.DomainFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task OpenDomainDetailsAsync()
    {
        await this.Page.GotoAsync($"/admin/domains/{this.reviewDomainId}");
        await Assertions.Expect(this.Page.GetByText(this.reviewDomainName, new() { Exact = true })).ToBeVisibleAsync();
        // Server-rendered controls become interactive after the Blazor circuit connects.
        await this.Page.WaitForTimeoutAsync(750);
    }

    [Given("I administer several domains with different states")]
    public async Task GivenSeveralDomainsAsync()
    {
        await this.PrepareDomainAsync(seed: false);
        var prefix = $"review-{Guid.NewGuid():N}";
        this.reviewDomainName = prefix;
        for (var index = 0; index < 22; index++)
        {
            await this.DomainFixture.SeedDomainAsync($"{prefix}-{index:D2}.example.test", createdAt: DateTime.UtcNow.AddMinutes(-index));
        }
        await this.DomainFixture.SeedDomainAsync($"{prefix}-verified.example.test", verified: true);
        await this.DomainFixture.SeedDomainAsync($"{prefix}-suspended.example.test", verified: true, suspended: true);
        await this.DomainFixture.SeedDomainAsync($"unrelated-{Guid.NewGuid():N}.example.test");
    }

    [When("I search for the fixture domain name and filter to pending unverified domains")]
    public async Task WhenISearchDomainsAsync()
    {
        await this.Page.GotoAsync("/admin/domains");
        await this.Page.GetByPlaceholder("Search by domain or contact...").FillAsync(this.reviewDomainName);
        await this.Page.Locator("select").Nth(0).SelectOptionAsync("Pending");
        await this.Page.Locator("select").Nth(1).SelectOptionAsync("Unverified");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
    }

    [Then("only the matching pending domains appear")]
    public async Task ThenOnlyPendingDomainsAsync()
    {
        var rows = this.Page.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(20);
        await Assertions.Expect(rows.First).ToContainTextAsync($"{this.reviewDomainName}-00.example.test");
        await Assertions.Expect(rows.First).ToContainTextAsync("Unverified");
        await Assertions.Expect(this.Page.GetByText($"{this.reviewDomainName}-verified.example.test")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText($"{this.reviewDomainName}-suspended.example.test")).ToHaveCountAsync(0);
    }

    [Then("I can navigate to the next page of matching domains")]
    public async Task ThenNextDomainPageAsync()
    {
        await this.Page.Locator("nav[aria-label='Pagination'] button").Last.ClickAsync();
        var rows = this.Page.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(2);
        await Assertions.Expect(rows.First).ToContainTextAsync($"{this.reviewDomainName}-20.example.test");
        await Assertions.Expect(rows.Last).ToContainTextAsync($"{this.reviewDomainName}-21.example.test");
    }

    [Given("I have domain administration permission")]
    public Task GivenIAmDomainAdminAsync() => this.PrepareDomainAsync(seed: false);

    [When("I add a valid domain with no contact email")]
    public async Task WhenIAddDomainAsync()
    {
        this.reviewDomainName = $"added-{Guid.NewGuid():N}.com";
        await this.Page.GotoAsync("/admin/domains");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Domain", Exact = true }).ClickAsync();
        await this.Page.GetByPlaceholder("example.com", new() { Exact = true }).FillAsync(this.reviewDomainName);
        await this.Page.Locator("button[type='submit']").Last.ClickAsync();
        await Assertions.Expect(this.Page.GetByText("DNS TXT Record Details")).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "I'll Verify Later" }).ClickAsync();
    }

    [Then("it appears as pending and unverified")]
    public async Task ThenDomainPendingAsync()
    {
        await this.Page.GetByPlaceholder("Search by domain or contact...").FillAsync(this.reviewDomainName);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
        var row = this.Page.Locator("tbody tr").Filter(new() { HasText = this.reviewDomainName });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await Assertions.Expect(row).ToContainTextAsync("Pending");
        await Assertions.Expect(row).ToContainTextAsync("Unverified");
    }

    [Then("I can open its details")]
    public async Task ThenOpenAddedDomainAsync()
    {
        await this.Page.Locator("tbody tr").Filter(new() { HasText = this.reviewDomainName })
            .GetByRole(AriaRole.Button, new() { Name = "View" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.reviewDomainName })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Verify domain to enable subdomains")).ToBeVisibleAsync();
    }

    [Given("I moderate a domain without system domain administration permission")]
    public Task GivenIAmDomainModeratorAsync() => this.PrepareDomainAsync(administrator: false, assigned: true, verified: true);

    [When("I open the domain list")]
    public Task WhenIOpenDomainListAsync() => this.Page.GotoAsync("/admin/domains");

    [Then("the add-domain action is unavailable")]
    public async Task ThenNoAddDomainAsync()
    {
        await Assertions.Expect(this.Page.GetByText(this.reviewDomainName)).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Domain" })).ToHaveCountAsync(0);
    }

    [Given("I administer an unverified domain")]
    public Task GivenUnverifiedDomainAsync() => this.PrepareDomainAsync();

    [When("I publish its verification TXT record and check verification")]
    public async Task WhenIPublishAndVerifyAsync()
    {
        this.DomainFixture.PublishTxt(this.reviewDomainName, this.verificationToken);
        await this.OpenDomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Check Verification" }).ClickAsync();
    }

    [Then("the domain is verified and subdomain creation is enabled")]
    public async Task ThenVerifiedAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Subdomain" })).ToBeEnabledAsync();
        await Assertions.Expect(this.Page.GetByText("Verify domain to enable subdomains")).ToHaveCountAsync(0);
    }

    [When("I check verification without publishing its TXT record")]
    public async Task WhenIVerifyWithoutDnsAsync()
    {
        await this.OpenDomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Check Verification" }).ClickAsync();
    }

    [Then("the domain remains unverified")]
    public async Task ThenUnverifiedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Verify domain to enable subdomains")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Subdomain" })).ToBeDisabledAsync();
    }

    [When("I open its details")]
    public Task WhenIOpenDomainDetailsAsync() => this.OpenDomainDetailsAsync();

    [Then("add-subdomain and add-moderator actions are disabled")]
    public async Task ThenUnverifiedActionsDisabledAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Subdomain" })).ToBeDisabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Moderator" })).ToBeDisabledAsync();
    }

    [Given("I administer a verified domain")]
    public Task GivenVerifiedDomainAsync() => this.PrepareDomainAsync(verified: true);

    [When("I change its contact and description")]
    public async Task WhenIEditDomainAsync()
    {
        await this.OpenDomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Domain" }).ClickAsync();
        var edit = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit Domain" });
        await edit.Locator("input").Last.FillAsync("updated@example.test");
        await edit.Locator("textarea").FillAsync("Updated domain description");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
    }

    [Then("the updated details are shown")]
    public async Task ThenUpdatedDomainAsync()
    {
        await Assertions.Expect(this.Page.GetByText("updated@example.test")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Updated domain description")).ToBeVisibleAsync();
    }

    [When("I suspend it with a reason")]
    public async Task WhenISuspendDomainAsync()
    {
        await this.OpenDomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Suspend Domain", Exact = true }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Suspend Domain" });
        await dialog.Locator("select").SelectOptionAsync("Administrative");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Suspend Domain", Exact = true }).ClickAsync();
    }

    [Then("it is suspended and management actions are disabled")]
    public async Task ThenSuspendedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("This domain is suspended")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Domain" })).ToBeDisabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Subdomain" })).ToBeDisabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Moderator" })).ToBeDisabledAsync();
    }

    [When("I restore it")]
    public async Task WhenIRestoreDomainAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Unsuspend Domain", Exact = true }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Unsuspend Domain" })
            .GetByRole(AriaRole.Button, new() { Name = "Unsuspend Domain", Exact = true }).ClickAsync();
    }

    [Then("it is active and management actions are enabled")]
    public async Task ThenRestoredAsync()
    {
        await Assertions.Expect(this.Page.GetByText("This domain is suspended")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Domain" })).ToBeEnabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Subdomain" })).ToBeEnabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Moderator" })).ToBeEnabledAsync();
    }

    [Given("I administer a verified domain and another user exists")]
    public async Task GivenModeratorCandidateAsync()
    {
        await this.PrepareDomainAsync(verified: true);
        this.moderatorName = $"Review moderator {Guid.NewGuid():N}";
        (_, this.moderatorEmail) = await this.DomainFixture.SeedUserAsync(this.moderatorName);
    }

    [When("I assign that user as a domain moderator")]
    public async Task WhenIAssignModeratorAsync()
    {
        await this.OpenDomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Moderator" }).ClickAsync();
        await this.Page.GetByPlaceholder("Search by name or email").FillAsync(this.moderatorEmail);
        await this.Page.GetByText(this.moderatorEmail).Last.ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Assign User" }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Moderators" }).ClickAsync();
    }

    [Then("the user appears in the domain's moderators tab")]
    public async Task ThenModeratorShownAsync()
    {
        for (var attempt = 0; attempt < 10 && await this.Page.GetByText(this.moderatorEmail).CountAsync() == 0; attempt++)
        {
            await this.Page.WaitForTimeoutAsync(300);
            await this.Page.ReloadAsync();
            await this.Page.WaitForTimeoutAsync(750);
            await this.Page.GetByRole(AriaRole.Button, new() { Name = "Moderators" }).ClickAsync();
        }

        await Assertions.Expect(this.Page.GetByText(this.moderatorEmail)).ToBeVisibleAsync();
    }

    [When("I remove the assignment")]
    public async Task WhenIRemoveModeratorAsync()
    {
        await this.Page.Locator("tr").Filter(new() { HasText = this.moderatorEmail })
            .GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).Last.ClickAsync();
    }

    [Then("the user no longer appears in the moderators tab")]
    public async Task ThenModeratorRemovedAsync() =>
        await Assertions.Expect(this.Page.GetByText(this.moderatorEmail)).ToHaveCountAsync(0);

    [Given("another user administers a domain outside my assignments")]
    public Task GivenUnrelatedDomainAsync() => this.PrepareDomainAsync(administrator: false);

    [When("I open its direct URL")]
    public Task WhenIOpenUnrelatedDomainAsync() => this.Page.GotoAsync($"/admin/domains/{this.reviewDomainId}");

    [Then("its details and management actions are not disclosed")]
    public async Task ThenDomainNotDisclosedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Domain not found")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.reviewDomainName)).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Domain" })).ToHaveCountAsync(0);
    }
}
