using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Spamma.Modules.Common.Client;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? settingsDomainFixture;
    private InboxScenarioFixture? settingsInboxFixture;
    private bool expectedCatchAllState;
    private string requestedAdminPage = string.Empty;
    private int maintenanceResponseStatus;
    private string maintenanceRedirect = string.Empty;

    [AfterScenario("settings")]
    public async Task DisposeSettingsFixturesAsync()
    {
        if (this.settingsInboxFixture is not null) await this.settingsInboxFixture.DisposeAsync();
        if (this.settingsDomainFixture is not null) await this.settingsDomainFixture.DisposeAsync();
    }

    private async Task SignInSettingsAsync(string email, Func<Task<string>> loginPath)
    {
        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(email);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await loginPath());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task PrepareSettingsDomainUserAsync(bool administrator = false, bool moderator = false)
    {
        this.settingsDomainFixture = await DomainScenarioFixture.CreateAsync(administrator);
        if (moderator)
        {
            var domainName = $"settings-{Guid.NewGuid():N}.test";
            var (domainId, _, _) = await this.settingsDomainFixture.SeedDomainAsync(domainName, verified: true);
            var subdomainId = await this.settingsDomainFixture.SeedSubdomainAsync(domainId, "mail");
            await this.settingsDomainFixture.AssignCurrentUserToSubdomainAsync(subdomainId);
        }
        await this.SignInSettingsAsync(this.settingsDomainFixture.EmailAddress,
            this.settingsDomainFixture.WaitForLoginPathAsync);
    }

    private async Task OpenSettingsPageAsync()
    {
        await this.Page.GotoAsync("/admin/settings");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Settings", Exact = true })).ToBeVisibleAsync();
    }

    [Given("catch-all mode is (.*) for a settings administrator")]
    public async Task GivenCatchAllStateForSettingsAsync(string state)
    {
        this.expectedCatchAllState = state switch
        {
            "enabled" => true,
            "disabled" => false,
            _ => throw new ArgumentException("Unknown catch-all state", nameof(state)),
        };
        this.settingsInboxFixture = await InboxScenarioFixture.CreateAsync(
            restrictedCampaignUser: true,
            isolatedRole: SystemRole.DomainManagement | SystemRole.UserManagement);
        await this.settingsInboxFixture.SetCatchAllModeAsync(this.expectedCatchAllState);
        await this.SignInSettingsAsync(this.settingsInboxFixture.EmailAddress,
            this.settingsInboxFixture.WaitForLoginPathAsync);
    }

    [When("I view the settings page")]
    public Task WhenIViewSettingsAsync() => this.OpenSettingsPageAsync();

    [Then("the catch-all setting shows (.*)")]
    public async Task ThenCatchAllSettingShowsStateAsync(string state)
    {
        Assert.Equal(this.expectedCatchAllState ? "enabled" : "disabled", state);
        await Assertions.Expect(this.Page.GetByTestId("catch-all-toggle"))
            .ToHaveAttributeAsync("aria-checked", this.expectedCatchAllState ? "true" : "false");
        await Assertions.Expect(this.Page.GetByText(this.expectedCatchAllState
            ? "Catch-All Mode is enabled."
            : "Catch-All Mode is disabled.", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Given("I can administer application settings")]
    public Task GivenICanAdministerSettingsAsync() => this.PrepareSettingsDomainUserAsync(administrator: true);

    [When("I choose to enter maintenance mode")]
    public async Task WhenIChooseMaintenanceAsync()
    {
        await this.OpenSettingsPageAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Enter Maintenance Mode", Exact = true }).ClickAsync();
    }

    [Then("I see a warning before the mode changes")]
    public async Task ThenMaintenanceWarningAsync()
    {
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Enter Maintenance Mode?" });
        await Assertions.Expect(dialog).ToContainTextAsync("all users will lose access until setup is complete");
        await Assertions.Expect(dialog).ToContainTextAsync("server logs");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await this.OpenSettingsPageAsync();
    }

    [Then("maintenance mode is enabled only after I confirm")]
    public async Task ThenMaintenanceAfterConfirmationAsync()
    {
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Enter Maintenance Mode", Exact = true }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Enter Maintenance Mode?" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Enter Maintenance Mode" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/setup-login$"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Spamma Setup Access" })).ToBeVisibleAsync();
    }

    [Given("I moderate a subdomain without global administration")]
    public Task GivenSubdomainModeratorForSettingsAsync() => this.PrepareSettingsDomainUserAsync(moderator: true);

    [When("I open the settings menu")]
    public async Task WhenIOpenSettingsMenuAsync()
    {
        await this.Page.Locator(".settings-dropdown-container button").First.ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Menu)).ToBeVisibleAsync();
    }

    [Then("I see my account and subdomain management links")]
    public async Task ThenRelevantSettingsLinksAsync()
    {
        var menu = this.Page.GetByRole(AriaRole.Menu);
        foreach (var name in new[] { "Passkeys", "API Keys", "Subdomain Management", "Chaos Addresses" })
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = name })).ToBeVisibleAsync();
    }

    [Then("global administration links are hidden")]
    public async Task ThenGlobalSettingsLinksHiddenAsync()
    {
        var menu = this.Page.GetByRole(AriaRole.Menu);
        foreach (var name in new[] { "User Management", "Domain Management", "Catch-All Senders", "Settings" })
            await Assertions.Expect(menu.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true })).ToHaveCountAsync(0);
    }

    [Given("I am signed in without global administration")]
    public Task GivenRestrictedSettingsUserAsync() => this.PrepareSettingsDomainUserAsync();

    [When("I enter the (.*) URL directly")]
    public async Task WhenIEnterAdminUrlDirectlyAsync(string page)
    {
        this.requestedAdminPage = page;
        var path = page switch
        {
            "user management" => "/admin/users",
            "application setup" => "/admin/settings",
            "catch-all senders" => "/admin/catch-all-senders",
            _ => throw new ArgumentException("Unknown administration page", nameof(page)),
        };
        await this.Page.GotoAsync(path);
    }

    [Then("I cannot view the (.*) administration page")]
    public async Task ThenAdminPageIsDeniedAsync(string page)
    {
        Assert.Equal(this.requestedAdminPage, page);
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/access-denied\?ReturnUrl="));
        await Assertions.Expect(this.Page.GetByText("You do not have permission to view this page.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Enter Maintenance Mode" })).ToHaveCountAsync(0);
    }

    [When("I request maintenance mode directly")]
    public async Task WhenIRequestMaintenanceDirectlyAsync()
    {
        var response = await this.Page.Context.APIRequest.PostAsync("/api/admin/maintenance",
            new() { MaxRedirects = 0 });
        this.maintenanceResponseStatus = response.Status;
        this.maintenanceRedirect = response.Headers.TryGetValue("location", out var location) ? location : string.Empty;
    }

    [Then("the request is forbidden and the app remains available")]
    public async Task ThenMaintenanceRequestForbiddenAsync()
    {
        Assert.True(this.maintenanceResponseStatus == 403 ||
            (this.maintenanceResponseStatus == 302 && this.maintenanceRedirect.Contains("access-denied", StringComparison.OrdinalIgnoreCase)),
            $"Expected a forbidden response or access-denied redirect, got {this.maintenanceResponseStatus} {this.maintenanceRedirect}.");
        await this.Page.GotoAsync("/m/inbox");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
    }
}
