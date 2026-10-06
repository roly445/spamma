using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? subdomainFixture;
    private Guid reviewParentId;
    private Guid reviewSubdomainId;
    private string reviewSubdomainName = string.Empty;
    private string reviewParentName = string.Empty;
    private string candidateEmail = string.Empty;

    private DomainScenarioFixture SubdomainFixture => this.subdomainFixture
        ?? throw new InvalidOperationException("The subdomain fixture has not been created.");

    [AfterScenario("subdomain")]
    public async Task DisposeSubdomainFixtureAsync()
    {
        if (this.subdomainFixture is not null) await this.subdomainFixture.DisposeAsync();
    }

    private async Task PrepareSubdomainAsync(bool administrator = true, bool seed = true)
    {
        this.subdomainFixture = await DomainScenarioFixture.CreateAsync(administrator);
        this.reviewParentName = $"review-{Guid.NewGuid():N}.com";
        (this.reviewParentId, _, _) = await this.SubdomainFixture.SeedDomainAsync(this.reviewParentName, verified: true);
        if (seed)
        {
            this.reviewSubdomainName = $"mail-{Guid.NewGuid():N}"[..13];
            this.reviewSubdomainId = await this.SubdomainFixture.SeedSubdomainAsync(
                this.reviewParentId, this.reviewSubdomainName, "Original subdomain description");
            if (!administrator) await this.SubdomainFixture.AssignCurrentUserToSubdomainAsync(this.reviewSubdomainId);
        }

        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.SubdomainFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.SubdomainFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/m/inbox$"));
    }

    private string ReviewFullName => $"{this.reviewSubdomainName}.{this.reviewParentName}";

    private async Task OpenSubdomainDetailsAsync()
    {
        await this.Page.GotoAsync($"/admin/subdomains/{this.reviewSubdomainId}");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.ReviewFullName })).ToBeVisibleAsync();
        await this.Page.WaitForTimeoutAsync(750);
    }

    [Given("I administer several subdomains with different states and parents")]
    public async Task GivenSeveralSubdomainsAsync()
    {
        await this.PrepareSubdomainAsync(seed: false);
        this.reviewSubdomainName = $"review-{Guid.NewGuid():N}";
        for (var index = 0; index < 22; index++)
        {
            await this.SubdomainFixture.SeedSubdomainAsync(this.reviewParentId,
                $"{this.reviewSubdomainName}-{index:D2}", createdAt: DateTime.UtcNow.AddMinutes(-index));
        }
        await this.SubdomainFixture.SeedSubdomainAsync(this.reviewParentId,
            $"{this.reviewSubdomainName}-suspended", suspended: true);
        var (otherParent, _, _) = await this.SubdomainFixture.SeedDomainAsync(
            $"other-{Guid.NewGuid():N}.com", verified: true);
        await this.SubdomainFixture.SeedSubdomainAsync(otherParent, $"{this.reviewSubdomainName}-elsewhere");
    }

    [When("I search for the fixture subdomain name and filter to active subdomains under its parent domain")]
    public async Task WhenISearchSubdomainsAsync()
    {
        await this.Page.GotoAsync("/admin/subdomains");
        await this.Page.GetByPlaceholder("Search by subdomain or description...").FillAsync(this.reviewSubdomainName);
        await this.Page.Locator("select").Nth(0).SelectOptionAsync("Active");
        await this.Page.Locator("select").Nth(1).SelectOptionAsync(this.reviewParentId.ToString());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
    }

    [Then("only the matching active subdomains appear")]
    public async Task ThenMatchingActiveSubdomainsAsync()
    {
        var rows = this.Page.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(20);
        await Assertions.Expect(rows.First).ToContainTextAsync($"{this.reviewSubdomainName}-00.{this.reviewParentName}");
        await Assertions.Expect(rows.First).ToContainTextAsync("Active");
        await Assertions.Expect(this.Page.GetByText($"{this.reviewSubdomainName}-suspended.{this.reviewParentName}")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText($"{this.reviewSubdomainName}-elsewhere", new() { Exact = false })).ToHaveCountAsync(0);
    }

    [Then("I can navigate to the next page of matching subdomains")]
    public async Task ThenNextSubdomainPageAsync()
    {
        await this.Page.Locator("nav[aria-label='Pagination'] button").Last.ClickAsync();
        var rows = this.Page.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(2);
        await Assertions.Expect(rows.First).ToContainTextAsync($"{this.reviewSubdomainName}-20.{this.reviewParentName}");
        await Assertions.Expect(rows.Last).ToContainTextAsync($"{this.reviewSubdomainName}-21.{this.reviewParentName}");
    }

    [Given("I administer a verified parent domain")]
    public Task GivenVerifiedParentAsync() => this.PrepareSubdomainAsync(seed: false);

    [When("I add a valid subdomain")]
    public async Task WhenIAddSubdomainAsync()
    {
        this.reviewSubdomainName = $"added-{Guid.NewGuid():N}"[..14];
        await this.Page.GotoAsync("/admin/subdomains");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Subdomain", Exact = true }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Dialog).Locator("select").SelectOptionAsync(this.reviewParentId.ToString());
        await this.Page.GetByPlaceholder("mail", new() { Exact = true }).FillAsync(this.reviewSubdomainName);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Create Subdomain" }).ClickAsync();
    }

    [Then("it appears beneath that domain")]
    public async Task ThenSubdomainAppearsAsync()
    {
        var row = this.Page.Locator("tbody tr").Filter(new() { HasText = this.ReviewFullName });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await Assertions.Expect(row).ToContainTextAsync("Active");
    }

    [Then("I can open the new subdomain details")]
    public async Task ThenOpenNewSubdomainAsync()
    {
        await this.Page.Locator("tbody tr").Filter(new() { HasText = this.ReviewFullName })
            .GetByRole(AriaRole.Button, new() { Name = "View" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.ReviewFullName })).ToBeVisibleAsync();
    }

    [Given("I moderate a subdomain")]
    [Given("I moderate an active subdomain")]
    public Task GivenModeratedSubdomainAsync() => this.PrepareSubdomainAsync(administrator: false);

    [When("I update its description")]
    public async Task WhenIEditSubdomainAsync()
    {
        await this.OpenSubdomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Subdomain" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Subdomain name cannot be changed after creation")).ToBeVisibleAsync();
        await this.Page.GetByPlaceholder("Optional description for this subdomain...").FillAsync("Updated subdomain description");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
    }

    [Then("its details show the saved description and unchanged name")]
    public async Task ThenEditedSubdomainAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Updated subdomain description")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.ReviewFullName })).ToBeVisibleAsync();
    }

    [When("I suspend the subdomain with a reason")]
    public async Task WhenISuspendSubdomainAsync()
    {
        await this.OpenSubdomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Suspend Subdomain", Exact = true }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Suspend Subdomain" });
        await dialog.Locator("select").SelectOptionAsync("AdminRequest");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Suspend Subdomain", Exact = true }).ClickAsync();
    }

    [Then("the subdomain is shown as suspended")]
    public async Task ThenSubdomainSuspendedAsync() =>
        await Assertions.Expect(this.Page.GetByText("This subdomain is suspended")).ToBeVisibleAsync();

    [When("I restore the subdomain")]
    public async Task WhenIRestoreSubdomainAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Unsuspend Subdomain", Exact = true }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Unsuspend Subdomain" })
            .GetByRole(AriaRole.Button, new() { Name = "Unsuspend Subdomain", Exact = true }).ClickAsync();
    }

    [Then("the subdomain is shown as active again")]
    public async Task ThenSubdomainRestoredAsync()
    {
        await Assertions.Expect(this.Page.GetByText("This subdomain is suspended")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Suspend Subdomain" })).ToBeVisibleAsync();
    }

    [Given("I moderate a subdomain and another user exists")]
    public async Task GivenCandidateUserAsync()
    {
        await this.PrepareSubdomainAsync(administrator: false);
        (_, this.candidateEmail) = await this.SubdomainFixture.SeedUserAsync($"Subdomain candidate {Guid.NewGuid():N}");
    }

    private async Task AssignCandidateAsync(string role, string tab)
    {
        await this.OpenSubdomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = $"Add {role}" }).ClickAsync();
        await this.Page.GetByPlaceholder("Search by name or email").FillAsync(this.candidateEmail);
        await this.Page.GetByText(this.candidateEmail).Last.ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Assign User" }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = tab, Exact = true }).ClickAsync();
    }

    [When("I assign that user as a subdomain moderator")]
    public Task WhenIAssignSubdomainModeratorAsync() => this.AssignCandidateAsync("Moderator", "Moderators");

    [When("I assign that user as a subdomain viewer")]
    public Task WhenIAssignSubdomainViewerAsync() => this.AssignCandidateAsync("Viewer", "Viewers");

    private async Task AssertCandidateVisibleAsync(string tab)
    {
        for (var attempt = 0; attempt < 10 && await this.Page.GetByText(this.candidateEmail).CountAsync() == 0; attempt++)
        {
            await this.Page.WaitForTimeoutAsync(300);
            await this.Page.ReloadAsync();
            await this.Page.WaitForTimeoutAsync(750);
            await this.Page.GetByRole(AriaRole.Button, new() { Name = tab, Exact = true }).ClickAsync();
        }
        await Assertions.Expect(this.Page.GetByText(this.candidateEmail)).ToBeVisibleAsync();
    }

    [Then("that user appears in the subdomain moderators tab")]
    public Task ThenModeratorVisibleAsync() => this.AssertCandidateVisibleAsync("Moderators");

    [Then("that user appears in the subdomain viewers tab")]
    public Task ThenViewerVisibleAsync() => this.AssertCandidateVisibleAsync("Viewers");

    [When("I remove the subdomain assignment")]
    public async Task WhenIRemoveSubdomainAssignmentAsync()
    {
        await this.Page.Locator("tr").Filter(new() { HasText = this.candidateEmail })
            .GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).Last.ClickAsync();
    }

    [Then("that user no longer appears in the subdomain moderators tab")]
    [Then("that user no longer appears in the subdomain viewers tab")]
    public async Task ThenCandidateRemovedAsync() =>
        await Assertions.Expect(this.Page.GetByText(this.candidateEmail)).ToHaveCountAsync(0);

    [Given("I can view a subdomain")]
    public Task GivenViewableSubdomainAsync() => this.PrepareSubdomainAsync();

    [When("I publish its MX record and request a check")]
    public async Task WhenIPublishAndCheckMxAsync()
    {
        this.SubdomainFixture.PublishMx(this.ReviewFullName, "mail.example.test");
        await this.OpenSubdomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Re-check MX Records" }).ClickAsync();
    }

    [When("I request a check without publishing its MX record")]
    public async Task WhenICheckMissingMxAsync()
    {
        await this.OpenSubdomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Re-check MX Records" }).ClickAsync();
    }

    [Then("its MX status is valid")]
    public async Task ThenMxValidAsync() =>
        await Assertions.Expect(this.Page.GetByText("MX Valid")).ToBeVisibleAsync();

    [Then("its MX status is invalid")]
    public async Task ThenMxInvalidAsync() =>
        await Assertions.Expect(this.Page.GetByText("No MX Records")).ToBeVisibleAsync();

    [Given("a subdomain is outside my assignments")]
    public async Task GivenUnrelatedSubdomainAsync()
    {
        await this.PrepareSubdomainAsync(administrator: false, seed: false);
        this.reviewSubdomainName = $"other-{Guid.NewGuid():N}"[..13];
        this.reviewSubdomainId = await this.SubdomainFixture.SeedSubdomainAsync(this.reviewParentId, this.reviewSubdomainName);
    }

    [When("I open the subdomain direct URL")]
    public Task WhenIOpenUnrelatedSubdomainAsync() => this.Page.GotoAsync($"/admin/subdomains/{this.reviewSubdomainId}");

    [Then("the unrelated subdomain details and management actions are not disclosed")]
    public async Task ThenSubdomainNotDisclosedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Subdomain not found")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.ReviewFullName)).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Subdomain" })).ToHaveCountAsync(0);
    }
}
