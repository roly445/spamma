using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Spamma.Modules.Common.Client;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private InboxScenarioFixture? catchAllFixture;
    private string catchAllSenderAddress = string.Empty;
    private string catchAllSubject = string.Empty;
    private string unrelatedCatchAllSubject = string.Empty;
    private Guid otherCatchAllMessageId;
    private string assignedUserEmail = string.Empty;

    private InboxScenarioFixture CatchAllFixture => this.catchAllFixture
        ?? throw new InvalidOperationException("The catch-all fixture has not been created.");

    [AfterScenario("catch-all")]
    public async Task DisposeCatchAllFixtureAsync()
    {
        if (this.catchAllFixture is not null) await this.catchAllFixture.DisposeAsync();
    }

    private async Task PrepareCatchAllAsync(bool administrator = false, bool enabled = false,
        Func<InboxScenarioFixture, Task>? seed = null)
    {
        var role = administrator ? SystemRole.DomainManagement | SystemRole.UserManagement : 0;
        this.catchAllFixture = await InboxScenarioFixture.CreateAsync(restrictedCampaignUser: true, isolatedRole: role);
        await this.CatchAllFixture.SetCatchAllModeAsync(enabled);
        if (seed is not null) await seed(this.CatchAllFixture);

        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.CatchAllFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.CatchAllFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private Task OpenCatchAllInboxAsync() => this.Page.GotoAsync("/m/catch-all");

    private async Task OpenSenderManagementAsync()
    {
        await this.Page.GotoAsync("/admin/catch-all-senders");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Catch-All Sender Addresses" })).ToBeVisibleAsync();
        await this.Page.WaitForTimeoutAsync(750);
    }

    private ILocator SenderRow() => this.Page.Locator("tbody tr").Filter(new() { HasText = this.catchAllSenderAddress });

    [Given("catch-all mode is disabled for a signed-in user")]
    public Task GivenCatchAllDisabledAsync() => this.PrepareCatchAllAsync();

    [When("I open the catch-all inbox")]
    public Task WhenIOpenCatchAllInboxAsync() => this.OpenCatchAllInboxAsync();

    [Then("I see that catch-all mode is disabled and where to enable it")]
    public async Task ThenCatchAllDisabledExplainedAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("disabled-state")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Catch-All Mode is disabled")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Ask an administrator to enable catch-all mode.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Go to Settings" })).ToHaveCountAsync(0);
    }

    [Given("I administer catch-all settings")]
    public Task GivenIAdministerCatchAllSettingsAsync() => this.PrepareCatchAllAsync(administrator: true);

    [When("I enable catch-all mode")]
    public async Task WhenIEnableCatchAllAsync()
    {
        await this.Page.GotoAsync("/admin/settings");
        await this.Page.GetByTestId("catch-all-toggle").ClickAsync();
    }

    [Then("catch-all routing is shown as enabled")]
    public async Task ThenCatchAllEnabledAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("catch-all-toggle")).ToHaveAttributeAsync("aria-checked", "true");
        await Assertions.Expect(this.Page.GetByText("Catch-All Mode is enabled.", new() { Exact = false })).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Catch-All Inbox" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Catch-All Inbox" })).ToBeVisibleAsync();
        await this.Page.GotoAsync("/admin/settings");
    }

    [When("I disable catch-all mode")]
    public Task WhenIDisableCatchAllAsync() => this.Page.GetByTestId("catch-all-toggle").ClickAsync();

    [Then("catch-all routing is shown as disabled")]
    public async Task ThenCatchAllDisabledAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("catch-all-toggle")).ToHaveAttributeAsync("aria-checked", "false");
        await Assertions.Expect(this.Page.GetByText("Catch-All Mode is disabled.", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Given("I administer catch-all sender addresses")]
    public Task GivenIAdministerCatchAllSendersAsync() => this.PrepareCatchAllAsync(administrator: true);

    [When("I add a valid sender address")]
    public async Task WhenIAddCatchAllSenderAsync()
    {
        this.catchAllSenderAddress = $"allowed-{Guid.NewGuid():N}@example.test";
        await this.OpenSenderManagementAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Address" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Add Sender Address" });
        await dialog.GetByLabel("Sender Address").FillAsync(this.catchAllSenderAddress);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Add Address" }).ClickAsync();
    }

    [Then("it appears in the sender address list")]
    public async Task ThenSenderListedAsync()
    {
        await this.Page.GetByPlaceholder("Search by sender address...").FillAsync(this.catchAllSenderAddress);
        await Assertions.Expect(this.SenderRow()).ToBeVisibleAsync();
        await Assertions.Expect(this.SenderRow()).ToContainTextAsync("0 users");
    }

    [When("I try to add an invalid sender address")]
    public async Task WhenIAddInvalidCatchAllSenderAsync()
    {
        await this.OpenSenderManagementAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Address" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Add Sender Address" });
        await dialog.GetByLabel("Sender Address").FillAsync("invalid-address");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Add Address" }).ClickAsync();
    }

    [Then("the address is rejected and the list is unchanged")]
    public async Task ThenInvalidSenderRejectedAsync()
    {
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Add Sender Address" });
        await Assertions.Expect(dialog.GetByText("Please enter a valid email address containing '@'.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("tbody tr").Filter(new() { HasText = "invalid-address" })).ToHaveCountAsync(0);
    }

    [Given("I administer an existing catch-all sender address and another user")]
    public Task GivenSenderAndUserAsync() => this.PrepareCatchAllAsync(administrator: true, seed: async fixture =>
    {
        this.catchAllSenderAddress = $"assignment-{Guid.NewGuid():N}@example.test";
        await fixture.SeedCatchAllSenderAsync(this.catchAllSenderAddress);
        (_, this.assignedUserEmail) = await fixture.SeedUserAsync();
    });

    [Given("I administer an existing catch-all sender address")]
    public Task GivenExistingSenderAsync() => this.PrepareCatchAllAsync(administrator: true, seed: async fixture =>
    {
        this.catchAllSenderAddress = $"removed-{Guid.NewGuid():N}@example.test";
        await fixture.SeedCatchAllSenderAsync(this.catchAllSenderAddress);
    });

    private async Task OpenSenderPanelAsync()
    {
        await this.OpenSenderManagementAsync();
        await this.Page.GetByPlaceholder("Search by sender address...").FillAsync(this.catchAllSenderAddress);
        await this.SenderRow().GetByRole(AriaRole.Button, new() { Name = "Manage" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Manage Sender Address" })).ToBeVisibleAsync();
    }

    [When("I assign that user to the address")]
    public async Task WhenIAssignCatchAllUserAsync()
    {
        await this.OpenSenderPanelAsync();
        var panel = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Manage Sender Address" });
        await panel.GetByPlaceholder("Search by email or name...").FillAsync(this.assignedUserEmail);
        await panel.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
        await panel.Locator("li").Filter(new() { HasText = this.assignedUserEmail })
            .GetByRole(AriaRole.Button, new() { Name = "Assign", Exact = true }).ClickAsync();
    }

    [Then("the user appears in its assigned users list")]
    public async Task ThenCatchAllUserAssignedAsync()
    {
        var panel = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Manage Sender Address" });
        await Assertions.Expect(panel.Locator("li").Filter(new() { HasText = this.assignedUserEmail })
            .GetByRole(AriaRole.Button, new() { Name = "Unassign" })).ToBeVisibleAsync();
        await Assertions.Expect(this.SenderRow()).ToContainTextAsync("1 user");
    }

    [When("I remove that assignment")]
    public Task WhenIUnassignCatchAllUserAsync() => this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Manage Sender Address" })
        .Locator("li").Filter(new() { HasText = this.assignedUserEmail })
        .GetByRole(AriaRole.Button, new() { Name = "Unassign" }).ClickAsync();

    [Then("the user no longer appears in its assigned users list")]
    public async Task ThenCatchAllUserUnassignedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("No users assigned yet.")).ToBeVisibleAsync();
        await Assertions.Expect(this.SenderRow()).ToContainTextAsync("0 users");
    }

    [When("I remove that sender address")]
    public async Task WhenIRemoveCatchAllSenderAsync()
    {
        await this.OpenSenderManagementAsync();
        await this.Page.GetByPlaceholder("Search by sender address...").FillAsync(this.catchAllSenderAddress);
        await this.SenderRow().GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
    }

    [Then("it no longer appears in the sender address list")]
    public Task ThenSenderRemovedAsync() => Assertions.Expect(this.SenderRow()).ToHaveCountAsync(0);

    [Given("I am assigned to an allowlisted sender with a captured message")]
    public Task GivenAssignedCatchAllMessageAsync() => this.PrepareCatchAllAsync(enabled: true, seed: async fixture =>
    {
        this.catchAllSenderAddress = $"assigned-{Guid.NewGuid():N}@example.test";
        var senderId = await fixture.SeedCatchAllSenderAsync(this.catchAllSenderAddress, assignedToCurrentUser: true);
        this.catchAllSubject = $"Assigned catch-all {Guid.NewGuid():N}";
        await fixture.SeedMessageAsync(this.catchAllSubject, this.catchAllSenderAddress, catchAllSenderAddressId: senderId);
    });

    [When("I open the catch-all inbox and select the message")]
    public async Task WhenISelectCatchAllMessageAsync()
    {
        await this.OpenCatchAllInboxAsync();
        await this.Page.GetByTestId("catch-all-email-row").Filter(new() { HasText = this.catchAllSubject }).ClickAsync();
    }

    [Then("I can inspect its sender, recipient, subject, and content")]
    public async Task ThenCatchAllContentVisibleAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = this.catchAllSubject })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.catchAllSenderAddress, new() { Exact = false }).Last).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("recipient@example.test", new() { Exact = false }).Last).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Text", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("Plain body for " + this.catchAllSubject)).ToBeVisibleAsync();
    }

    [Given("I can view catch-all messages with different subjects")]
    public Task GivenDifferentCatchAllMessagesAsync() => this.PrepareCatchAllAsync(enabled: true, seed: async fixture =>
    {
        this.catchAllSenderAddress = $"search-{Guid.NewGuid():N}@example.test";
        var senderId = await fixture.SeedCatchAllSenderAsync(this.catchAllSenderAddress, assignedToCurrentUser: true);
        this.catchAllSubject = $"Needle catch-all {Guid.NewGuid():N}";
        this.unrelatedCatchAllSubject = $"Unrelated catch-all {Guid.NewGuid():N}";
        await fixture.SeedMessageAsync(this.catchAllSubject, this.catchAllSenderAddress, catchAllSenderAddressId: senderId);
        await fixture.SeedMessageAsync(this.unrelatedCatchAllSubject, this.catchAllSenderAddress, catchAllSenderAddressId: senderId);
    });

    [When("I search for one catch-all message")]
    public async Task WhenISearchCatchAllAsync()
    {
        await this.OpenCatchAllInboxAsync();
        await this.Page.GetByPlaceholder("Search catch-all emails...").FillAsync(this.catchAllSubject);
        await this.Page.GetByPlaceholder("Search catch-all emails...").PressAsync("Enter");
    }

    [Then("only the matching catch-all message is shown")]
    public async Task ThenOnlyMatchingCatchAllAsync()
    {
        await Assertions.Expect(this.Page.GetByTestId("catch-all-email-row")).ToHaveCountAsync(1);
        await Assertions.Expect(this.Page.GetByText(this.catchAllSubject, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.unrelatedCatchAllSubject, new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Given("another user's catch-all sender has a captured message")]
    public Task GivenOtherCatchAllMessageAsync() => this.PrepareCatchAllAsync(enabled: true, seed: async fixture =>
    {
        this.catchAllSenderAddress = $"other-{Guid.NewGuid():N}@example.test";
        var otherSenderId = await fixture.SeedCatchAllSenderAsync(this.catchAllSenderAddress);
        this.catchAllSubject = $"Private catch-all {Guid.NewGuid():N}";
        this.otherCatchAllMessageId = await fixture.SeedMessageAsync(this.catchAllSubject, this.catchAllSenderAddress,
            catchAllSenderAddressId: otherSenderId);
    });

    [When("I search the catch-all inbox for that message")]
    public async Task WhenISearchOtherCatchAllAsync()
    {
        await this.OpenCatchAllInboxAsync();
        await this.Page.GetByPlaceholder("Search catch-all emails...").FillAsync(this.catchAllSubject);
        await this.Page.GetByPlaceholder("Search catch-all emails...").PressAsync("Enter");
    }

    [Then("it is not shown in my catch-all inbox")]
    public async Task ThenOtherCatchAllHiddenAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "No catch-all emails found" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.catchAllSubject, new() { Exact = true })).ToHaveCountAsync(0);
    }

    private IAPIResponse? otherCatchAllResponse;

    [When("I request the other catch-all message by ID")]
    public async Task WhenIRequestOtherCatchAllAsync() => this.otherCatchAllResponse = await this.Page.Context.APIRequest.GetAsync(
        $"/api/email-inbox/emails/{this.otherCatchAllMessageId}/mime-content");

    [Then("the other catch-all message is denied")]
    public void ThenOtherCatchAllDenied()
    {
        Assert.NotNull(this.otherCatchAllResponse);
        Assert.Equal(401, this.otherCatchAllResponse.Status);
    }

    [Given("I am signed in without catch-all administration permission")]
    public Task GivenNoCatchAllAdminAsync() => this.PrepareCatchAllAsync();

    [When("I open the settings and sender management URLs")]
    public async Task WhenIOpenCatchAllAdminUrlsAsync()
    {
        await this.Page.GotoAsync("/admin/settings");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/access-denied\?ReturnUrl="));
        await this.Page.GotoAsync("/admin/catch-all-senders");
    }

    [Then("I cannot change catch-all mode or sender addresses")]
    public async Task ThenCatchAllAdminDeniedAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/access-denied\?ReturnUrl="));
        await Assertions.Expect(this.Page.GetByText("You do not have permission to view this page.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByTestId("catch-all-toggle")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Address" })).ToHaveCountAsync(0);
    }
}
