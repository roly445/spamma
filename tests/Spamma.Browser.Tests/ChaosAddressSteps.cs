using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Spamma.Modules.Common.Client;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? chaosFixture;
    private Guid chaosDomainId;
    private Guid chaosSubdomainId;
    private string chaosSubdomainName = string.Empty;
    private string chaosDomainName = string.Empty;
    private string chaosLocalPart = string.Empty;
    private string otherChaosLocalPart = string.Empty;

    private DomainScenarioFixture ChaosFixture => this.chaosFixture
        ?? throw new InvalidOperationException("The chaos fixture has not been created.");

    private string ChaosAddress => $"{this.chaosLocalPart}@{this.chaosSubdomainName}.{this.chaosDomainName}";

    private ILocator ChaosRow => this.Page.Locator("tbody tr").Filter(new() { HasText = this.ChaosAddress });

    [AfterScenario("chaos")]
    public async Task DisposeChaosFixtureAsync()
    {
        if (this.chaosFixture is not null) await this.chaosFixture.DisposeAsync();
    }

    private async Task PrepareChaosAsync(bool administrator = false, bool viewer = false,
        Func<Task>? seed = null)
    {
        this.chaosFixture = await DomainScenarioFixture.CreateAsync(administrator);
        this.chaosDomainName = $"chaos-{Guid.NewGuid():N}.test";
        (this.chaosDomainId, _, _) = await this.ChaosFixture.SeedDomainAsync(this.chaosDomainName, verified: true);
        this.chaosSubdomainName = $"mail-{Guid.NewGuid():N}"[..13];
        this.chaosSubdomainId = await this.ChaosFixture.SeedSubdomainAsync(this.chaosDomainId, this.chaosSubdomainName);
        if (!administrator)
        {
            if (viewer) await this.ChaosFixture.AssignCurrentUserToViewSubdomainAsync(this.chaosSubdomainId);
            else await this.ChaosFixture.AssignCurrentUserToSubdomainAsync(this.chaosSubdomainId);
        }

        if (seed is not null) await seed();

        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.ChaosFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.ChaosFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task OpenChaosAsync()
    {
        await this.Page.GotoAsync("/chaos-addresses");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Chaos Addresses" })).ToBeVisibleAsync();
        await this.Page.WaitForTimeoutAsync(750);
    }

    private async Task SeedChaosAsync(bool enabled = false)
    {
        this.chaosLocalPart = $"chaos-{Guid.NewGuid():N}"[..18];
        await this.ChaosFixture.SeedChaosAddressAsync(this.chaosDomainId, this.chaosSubdomainId,
            this.chaosLocalPart, enabled);
    }

    private async Task FillCreateDialogAsync(string localPart)
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Create Chaos Address" }).First.ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Create Chaos Address" });
        await dialog.Locator("select").Nth(0).SelectOptionAsync(this.chaosSubdomainId.ToString());
        await dialog.Locator("#edit-localpart").FillAsync(localPart);
        await dialog.Locator("select").Nth(1).SelectOptionAsync(((int)SmtpResponseCode.MailboxUnavailablePermanent).ToString());
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Chaos Address" }).ClickAsync();
    }

    [Given("I can moderate several chaos addresses")]
    public Task GivenSeveralChaosAddressesAsync() => this.PrepareChaosAsync(administrator: true, seed: async () =>
    {
        this.chaosLocalPart = $"match-{Guid.NewGuid():N}"[..17];
        this.otherChaosLocalPart = this.chaosLocalPart + "-other";
        await this.ChaosFixture.SeedChaosAddressAsync(this.chaosDomainId, this.chaosSubdomainId,
            this.chaosLocalPart, enabled: true);
        await this.ChaosFixture.SeedChaosAddressAsync(this.chaosDomainId, this.chaosSubdomainId,
            this.otherChaosLocalPart);
        var otherSubdomain = await this.ChaosFixture.SeedSubdomainAsync(this.chaosDomainId, "other-" + Guid.NewGuid().ToString("N")[..8]);
        await this.ChaosFixture.SeedChaosAddressAsync(this.chaosDomainId, otherSubdomain,
            this.chaosLocalPart + "-elsewhere", enabled: true);
    });

    [When("I search by address and filter by subdomain or status")]
    public async Task WhenISearchChaosAddressesAsync()
    {
        await this.OpenChaosAsync();
        await this.Page.GetByPlaceholder("Search by email address...").FillAsync(this.chaosLocalPart);
        await this.Page.Locator("form").First.Locator("select").Nth(0)
            .SelectOptionAsync(this.chaosSubdomainId.ToString());
        await this.Page.Locator("form").First.Locator("select").Nth(1)
            .SelectOptionAsync("enabled");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
    }

    [Then("I see only matching addresses")]
    public async Task ThenOnlyMatchingChaosAddressesAsync()
    {
        await Assertions.Expect(this.Page.Locator("tbody tr")).ToHaveCountAsync(1);
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("Enabled");
        await Assertions.Expect(this.Page.GetByText(this.otherChaosLocalPart, new() { Exact = false })).ToHaveCountAsync(0);
    }

    [Given("I can moderate a subdomain")]
    public Task GivenICanModerateChaosSubdomainAsync() => this.PrepareChaosAsync();

    [When("I create a chaos address for that subdomain")]
    public async Task WhenICreateChaosAddressAsync()
    {
        this.chaosLocalPart = $"created-{Guid.NewGuid():N}"[..18];
        await this.OpenChaosAsync();
        await this.FillCreateDialogAsync(this.chaosLocalPart);
    }

    [Then("it appears in the chaos address list")]
    public async Task ThenChaosAddressListedAsync()
    {
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("Disabled");
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("MailboxUnavailablePermanent");
    }

    [When("I try to create a chaos address with an invalid local part")]
    public async Task WhenICreateInvalidChaosAddressAsync()
    {
        await this.OpenChaosAsync();
        await this.FillCreateDialogAsync("invalid local part");
    }

    [Then("the invalid address is rejected and the list is unchanged")]
    public async Task ThenInvalidChaosAddressRejectedAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Create Chaos Address" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("tbody tr")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText("Failed to create chaos address")).ToBeVisibleAsync();
    }

    [Given("I moderate an enabled chaos address")]
    public Task GivenEnabledChaosAddressAsync() => this.PrepareChaosAsync(seed: () => this.SeedChaosAsync(enabled: true));

    [When("I choose to disable it")]
    public async Task WhenIChooseToDisableChaosAddressAsync()
    {
        await this.OpenChaosAsync();
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await this.ChaosRow.GetByRole(AriaRole.Button, new() { Name = "Disable", Exact = true }).ClickAsync();
    }

    [Then("I see the consequences before confirming")]
    public async Task ThenDisableConsequencesAsync()
    {
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Disable Chaos Address" });
        await Assertions.Expect(dialog.GetByText("Prevent receiving new emails for this address")).ToBeVisibleAsync();
        await Assertions.Expect(dialog.GetByText("Keep the address and its history available")).ToBeVisibleAsync();
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("Enabled");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("Enabled");
        await this.ChaosRow.GetByRole(AriaRole.Button, new() { Name = "Disable", Exact = true }).ClickAsync();
    }

    [Then("the address is disabled only after I confirm")]
    public async Task ThenChaosAddressDisabledAsync()
    {
        await this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Disable Chaos Address" })
            .GetByRole(AriaRole.Button, new() { Name = "Disable Address" }).ClickAsync();
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("Disabled");
    }

    [Given("I moderate a disabled chaos address")]
    public Task GivenDisabledChaosAddressAsync() => this.PrepareChaosAsync(seed: () => this.SeedChaosAsync());

    [When("I confirm that it should be enabled")]
    public async Task WhenIEnableChaosAddressAsync()
    {
        await this.OpenChaosAsync();
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await this.ChaosRow.GetByRole(AriaRole.Button, new() { Name = "Enable", Exact = true }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Enable Chaos Address" });
        await Assertions.Expect(dialog.GetByText("Apply the configured SMTP error code")).ToBeVisibleAsync();
        await Assertions.Expect(this.ChaosRow).ToContainTextAsync("Disabled");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Enable Address" }).ClickAsync();
    }

    [Then("it is shown as enabled")]
    public Task ThenChaosAddressEnabledAsync() => Assertions.Expect(this.ChaosRow).ToContainTextAsync("Enabled");

    [Given("I moderate a chaos address")]
    public Task GivenChaosAddressAsync() => this.PrepareChaosAsync(seed: () => this.SeedChaosAsync());

    [When("I choose to delete it")]
    public async Task WhenIChooseToDeleteChaosAddressAsync()
    {
        await this.OpenChaosAsync();
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await this.ChaosRow.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
    }

    [Then("I see that its received count will no longer be shown")]
    public async Task ThenDeleteConsequencesAsync()
    {
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete Chaos Address" });
        await Assertions.Expect(dialog.GetByText("Its received count will no longer be shown")).ToBeVisibleAsync();
        await Assertions.Expect(dialog.GetByText("Cannot be undone here")).ToBeVisibleAsync();
    }

    [Then("the address is removed only after I confirm")]
    public async Task ThenChaosAddressRemovedAsync()
    {
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete Chaos Address" });
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await this.ChaosRow.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete Address" }).ClickAsync();
        await Assertions.Expect(this.ChaosRow).ToHaveCountAsync(0);
    }

    [Given("I am signed in without chaos moderation permission")]
    public Task GivenChaosViewerAsync() => this.PrepareChaosAsync(viewer: true,
        seed: () => this.SeedChaosAsync(enabled: true));

    [When("I navigate to the chaos addresses URL")]
    public Task WhenIOpenChaosAsViewerAsync() => this.Page.GotoAsync("/chaos-addresses");

    [Then("I cannot create, enable, disable, or delete chaos addresses")]
    public async Task ThenChaosManagementDeniedAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/access-denied\?ReturnUrl="));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Create Chaos Address" })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.Locator("tbody tr")).ToHaveCountAsync(0);
    }

    [Given("I moderate one of two subdomains with chaos addresses")]
    public Task GivenChaosAddressesOnTwoSubdomainsAsync() => this.PrepareChaosAsync(seed: async () =>
    {
        await this.SeedChaosAsync(enabled: true);
        this.otherChaosLocalPart = $"private-{Guid.NewGuid():N}"[..18];
        var otherSubdomain = await this.ChaosFixture.SeedSubdomainAsync(this.chaosDomainId,
            "other-" + Guid.NewGuid().ToString("N")[..8]);
        await this.ChaosFixture.SeedChaosAddressAsync(this.chaosDomainId, otherSubdomain,
            this.otherChaosLocalPart, enabled: true);
    });

    [When("I open the chaos addresses list")]
    public Task WhenIOpenChaosListAsync() => this.OpenChaosAsync();

    [Then("I see only addresses for the subdomain I moderate")]
    public async Task ThenChaosListIsScopedAsync()
    {
        await Assertions.Expect(this.Page.Locator("tbody tr")).ToHaveCountAsync(1);
        await Assertions.Expect(this.ChaosRow).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.otherChaosLocalPart, new() { Exact = false })).ToHaveCountAsync(0);
    }
}
