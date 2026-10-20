using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? domainFixture;
    private Guid reviewDomainId;
    private Guid submissionSubdomainId;
    private string previousSubmissionUsername = string.Empty;
    private string reviewDomainName = string.Empty;
    private string verificationToken = string.Empty;
    private string moderatorEmail = string.Empty;
    private string moderatorName = string.Empty;
    private const string ScreenshotZoneName = "example.com";
    private readonly string screenshotIntegrationName = $"Screenshot Cloudflare {Guid.NewGuid().ToString("N")[..6]}";

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

    [Given("I administer a verified domain with a subdomain for SMTP submission")]
    public async Task GivenSubmissionDomainAsync()
    {
        await this.PrepareDomainAsync(verified: true);
        this.submissionSubdomainId = await this.DomainFixture.SeedSubdomainAsync(this.reviewDomainId, "team");
    }

    [When("I require an SMTP credential for the domain")]
    public async Task WhenIRequireDomainSmtpCredentialAsync()
    {
        await this.Page.GotoAsync($"/admin/domains/{this.reviewDomainId}/smtp-submission");
        await this.Page.Locator("#smtp-policy-mode").SelectOptionAsync("RequireAuthentication");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save policy" }).ClickAsync();
    }

    [Then("I receive the new SMTP credential once")]
    public async Task ThenIReceiveSmtpCredentialAsync()
    {
        await Assertions.Expect(this.Page.Locator("#smtp-submission-secret")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Credential required", new() { Exact = false })).ToBeVisibleAsync();
    }

    [When("I reopen the SMTP submission settings")]
    public Task WhenIReopenSmtpSettingsAsync() =>
        this.Page.GotoAsync($"/admin/domains/{this.reviewDomainId}/smtp-submission");

    [Then("the effective domain policy requires authentication without showing the password")]
    public async Task ThenDomainSmtpPolicyIsPersistedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Credential required", new() { Exact = false })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#smtp-submission-secret")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText("SMTP username")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Save policy" })).ToBeDisabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Rotate credential" })).ToBeVisibleAsync();
        var screenshotDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_SCREENSHOT_DIR");
        if (!string.IsNullOrWhiteSpace(screenshotDirectory))
        {
            Directory.CreateDirectory(screenshotDirectory);
            await this.Page.SetViewportSizeAsync(1280, 900);
            await Assertions.Expect(this.Page.Locator("#blazor-wasm-loading")).ToBeHiddenAsync();
            await this.Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(screenshotDirectory, "smtp-submission-policy.png"),
                FullPage = true,
            });
        }
    }

    [When("I open SMTP submission settings for the subdomain")]
    public Task WhenIOpenSubdomainSmtpSettingsAsync() =>
        this.Page.GotoAsync($"/admin/subdomains/{this.submissionSubdomainId}/smtp-submission");

    [Then("the subdomain inherits the domain policy")]
    public async Task ThenSubdomainInheritsSmtpPolicyAsync()
    {
        await Assertions.Expect(this.Page.Locator("#smtp-policy-mode")).ToHaveValueAsync("Inherit");
        await Assertions.Expect(this.Page.GetByText("(inherited from domain)")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Credential required", new() { Exact = false })).ToBeVisibleAsync();
    }

    [When("I disable SMTP authentication on the subdomain")]
    public async Task WhenIDisableSubdomainSmtpAuthenticationAsync()
    {
        await this.Page.Locator("#smtp-policy-mode").SelectOptionAsync("Disabled");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save policy" }).ClickAsync();
    }

    [Then("the subdomain permits anonymous TLS submission")]
    public async Task ThenSubdomainPermitsAnonymousSubmissionAsync()
    {
        await Assertions.Expect(this.Page.Locator("#smtp-effective-policy")).ToContainTextAsync("No authentication required");
        await Assertions.Expect(this.Page.GetByText("(subdomain override)")).ToBeVisibleAsync();
    }

    [When("I require a separate SMTP credential on the subdomain")]
    public async Task WhenIRequireSubdomainSmtpCredentialAsync()
    {
        await this.Page.Locator("#smtp-policy-mode").SelectOptionAsync("RequireAuthentication");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save policy" }).ClickAsync();
    }

    [Then("the subdomain shows its own credential and effective override")]
    public async Task ThenSubdomainSmtpCredentialOverridesAsync()
    {
        await Assertions.Expect(this.Page.Locator("#smtp-submission-secret")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("(subdomain override)")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("code").Filter(new() { HasText = $"subdomain:{this.submissionSubdomainId:N}" }))
            .ToBeVisibleAsync();
    }

    [When("I rotate the subdomain SMTP credential")]
    public async Task WhenIRotateSubdomainSmtpCredentialAsync()
    {
        this.previousSubmissionUsername = await this.Page.Locator("code").Filter(new() { HasText = $"subdomain:{this.submissionSubdomainId:N}" }).InnerTextAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Rotate credential" }).ClickAsync();
    }

    [Then("its username changes and the new password is shown once")]
    public async Task ThenRotatedSubdomainSmtpCredentialIsShownAsync()
    {
        await Assertions.Expect(this.Page.Locator("#smtp-submission-secret")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("code").Filter(new() { HasText = $"subdomain:{this.submissionSubdomainId:N}" }))
            .Not.ToHaveTextAsync(this.previousSubmissionUsername);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Save policy" })).ToBeDisabledAsync();
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

    [When("I configure a Cloudflare DNS integration")]
    public async Task WhenIConfigureCloudflareDnsIntegrationAsync()
    {
        await this.Page.GotoAsync("/admin/dns-integrations");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Integration" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Add DNS Integration" })).ToBeVisibleAsync();
        await this.SaveDnsScreenshotAsync("add-dns-integration.png");
        await this.Page.Locator("#dns-connection-name").FillAsync(this.screenshotIntegrationName);
        await this.Page.Locator("#dns-zone-name").FillAsync(ScreenshotZoneName);
        await this.Page.Locator("#dns-zone-id").FillAsync(Guid.NewGuid().ToString("N"));
        await this.Page.Locator("#dns-api-token").FillAsync("fixture-token-do-not-use");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Integration" }).ClickAsync();
    }

    [Then("the DNS integration appears without its token")]
    public async Task ThenDnsIntegrationAppearsWithoutTokenAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Integration saved. Select it when adding a matching domain.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.screenshotIntegrationName, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#dns-api-token")).ToHaveCountAsync(0);
        await this.SaveDnsScreenshotAsync("dns-integrations.png");
    }

    [When("I edit the DNS integration in the slide out")]
    public async Task WhenIEditDnsIntegrationAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = $"Edit {this.screenshotIntegrationName}" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit DNS Integration" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#edit-dns-api-token")).ToBeEmptyAsync();
        await this.SaveDnsScreenshotAsync("edit-dns-integration.png");
        await this.Page.Locator("#edit-dns-zone-id").FillAsync(new string('b', 32));
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
    }

    [Then("the updated DNS integration appears")]
    public async Task ThenUpdatedDnsIntegrationAppearsAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Integration updated.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("tbody tr").Filter(new() { HasText = this.screenshotIntegrationName }))
            .ToContainTextAsync(new string('b', 32));
    }

    [When("I open the add-domain form")]
    public async Task WhenIOpenAddDomainFormAsync()
    {
        await this.Page.GotoAsync("/admin/domains");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Domain", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.Locator("#domain-dns-integration")).ToBeVisibleAsync();
    }

    [Then("I can choose manual DNS or the Cloudflare integration")]
    public async Task ThenCanChooseDnsSetupAsync()
    {
        var selector = this.Page.Locator("#domain-dns-integration");
        await Assertions.Expect(selector).ToHaveValueAsync(string.Empty);
        await Assertions.Expect(selector.Locator("option").Filter(new() { HasText = this.screenshotIntegrationName })).ToHaveCountAsync(1);
        await selector.SelectOptionAsync(new SelectOptionValue { Label = $"{this.screenshotIntegrationName} (Cloudflare · {ScreenshotZoneName})" });
        await Assertions.Expect(selector).Not.ToHaveValueAsync(string.Empty);
        await this.SaveDnsScreenshotAsync("add-domain-dns-selection.png");
    }

    private async Task SaveDnsScreenshotAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("SPAMMA_DNS_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await this.Page.SetViewportSizeAsync(1280, 1000);
        await this.Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = true,
        });
    }

    [When("I configure a Cloudflare integration for that domain")]
    public async Task WhenIConfigureCloudflareForExistingDomainAsync()
    {
        await this.Page.GotoAsync("/admin/dns-integrations");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Integration" }).ClickAsync();
        await this.Page.Locator("#dns-connection-name").FillAsync(this.screenshotIntegrationName);
        await this.Page.Locator("#dns-zone-name").FillAsync(this.reviewDomainName);
        await this.Page.Locator("#dns-zone-id").FillAsync(Guid.NewGuid().ToString("N"));
        await this.Page.Locator("#dns-api-token").FillAsync("fixture-token-do-not-use");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Integration" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText(this.screenshotIntegrationName, new() { Exact = true })).ToBeVisibleAsync();
    }

    [When("I assign it in the domain edit panel")]
    public async Task WhenIAssignDnsIntegrationToExistingDomainAsync()
    {
        await this.OpenDomainDetailsAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Edit Domain" }).ClickAsync();
        var selector = this.Page.Locator("#edit-domain-dns-integration");
        await Assertions.Expect(selector).ToBeVisibleAsync();
        await selector.SelectOptionAsync(new SelectOptionValue { Label = $"{this.screenshotIntegrationName} (Cloudflare)" });
        await this.SaveDnsScreenshotAsync("existing-domain-dns-selection.png");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
    }

    [Then("automatic DNS setup is available for the existing domain")]
    public async Task ThenExistingDomainCanSetUpDnsAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Set up DNS automatically" })).ToBeVisibleAsync();
    }

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
