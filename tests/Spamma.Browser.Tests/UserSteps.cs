using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Spamma.Modules.Common.Client;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? userFixture;
    private Guid reviewUserId;
    private string reviewUserName = string.Empty;
    private string reviewUserEmail = string.Empty;

    private DomainScenarioFixture UserFixture => this.userFixture
        ?? throw new InvalidOperationException("The user fixture has not been created.");

    [AfterScenario("user")]
    public async Task DisposeUserFixtureAsync()
    {
        if (this.userFixture is not null) await this.userFixture.DisposeAsync();
    }

    private async Task PrepareUserAsync(bool administrator = true)
    {
        this.userFixture = await DomainScenarioFixture.CreateAsync(administrator);
        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.UserFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.UserFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task OpenUsersAsync()
    {
        await this.Page.GotoAsync("/admin/users");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "User Management" })).ToBeVisibleAsync();
        await this.Page.WaitForTimeoutAsync(750);
    }

    private async Task SearchUserAsync(string term, string? status = null)
    {
        await this.Page.GetByPlaceholder("Search by email or name...").FillAsync(term);
        if (status is not null) await this.Page.Locator("select").First.SelectOptionAsync(status);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();
    }

    private ILocator ReviewRow() => this.Page.Locator("tbody tr").Filter(new() { HasText = this.reviewUserEmail });

    [Given("I administer users with active, inactive, and suspended accounts")]
    public async Task GivenUsersWithStatesAsync()
    {
        await this.PrepareUserAsync();
        this.reviewUserName = $"Review users {Guid.NewGuid():N}";
        for (var index = 0; index < 22; index++)
        {
            await this.UserFixture.SeedUserAsync($"{this.reviewUserName} {index:D2}",
                lastLoginAt: DateTime.UtcNow.AddDays(-1), createdAt: DateTime.UtcNow.AddMinutes(-index));
        }
        await this.UserFixture.SeedUserAsync($"{this.reviewUserName} inactive");
        await this.UserFixture.SeedUserAsync($"{this.reviewUserName} suspended", suspended: true,
            lastLoginAt: DateTime.UtcNow.AddDays(-1));
    }

    [When("I search for active users by name")]
    public async Task WhenISearchActiveUsersAsync()
    {
        await this.OpenUsersAsync();
        await this.SearchUserAsync(this.reviewUserName, "Active");
    }

    [Then("only matching active users appear on the first page")]
    public async Task ThenOnlyActiveUsersAsync()
    {
        var rows = this.Page.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(20);
        await Assertions.Expect(rows.First).ToContainTextAsync($"{this.reviewUserName} 00");
        foreach (var index in Enumerable.Range(0, 20))
        {
            await Assertions.Expect(rows.Nth(index)).ToContainTextAsync("Active");
        }
        await Assertions.Expect(this.Page.GetByText($"{this.reviewUserName} inactive")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText($"{this.reviewUserName} suspended")).ToHaveCountAsync(0);
    }

    [Then("I can navigate to the remaining matching users")]
    public async Task ThenNextUsersPageAsync()
    {
        await this.Page.Locator("nav[aria-label='Pagination'] button").Last.ClickAsync();
        var rows = this.Page.Locator("tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(2);
        await Assertions.Expect(rows.First).ToContainTextAsync($"{this.reviewUserName} 20");
        await Assertions.Expect(rows.Last).ToContainTextAsync($"{this.reviewUserName} 21");
    }

    [Given("I have user administration permission")]
    public Task GivenIHaveUserAdministrationAsync() => this.PrepareUserAsync();

    [When("I add a user with valid details without sending an invitation")]
    public async Task WhenIAddUserAsync()
    {
        await this.OpenUsersAsync();
        this.reviewUserName = $"Added user {Guid.NewGuid():N}";
        this.reviewUserEmail = $"added-{Guid.NewGuid():N}@example.test";
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add User", Exact = true }).ClickAsync();
        await this.Page.GetByPlaceholder("user@example.com").FillAsync(this.reviewUserEmail);
        await this.Page.GetByPlaceholder("John Doe").FillAsync(this.reviewUserName);
        var addDialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Add New User" });
        await addDialog.Locator("input[type='checkbox']").Last.UncheckAsync();
        await addDialog.GetByRole(AriaRole.Button, new() { Name = "Add User", Exact = true }).ClickAsync();
    }

    [Then("the new account appears as inactive")]
    public async Task ThenNewUserInactiveAsync()
    {
        await this.SearchUserAsync(this.reviewUserEmail);
        await Assertions.Expect(this.ReviewRow()).ToBeVisibleAsync();
        await Assertions.Expect(this.ReviewRow()).ToContainTextAsync(this.reviewUserName);
        await Assertions.Expect(this.ReviewRow()).ToContainTextAsync("Inactive");
    }

    [Given("I administer a user with a domain management role")]
    public async Task GivenUserWithRoleAsync()
    {
        await this.PrepareUserAsync();
        this.reviewUserName = $"Editable user {Guid.NewGuid():N}";
        (this.reviewUserId, this.reviewUserEmail) = await this.UserFixture.SeedUserAsync(this.reviewUserName,
            role: SystemRole.DomainManagement);
    }

    [When("I edit that user's name and email")]
    public async Task WhenIEditUserAsync()
    {
        await this.OpenUsersAsync();
        await this.SearchUserAsync(this.reviewUserEmail);
        await this.ReviewRow().GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        this.reviewUserName += " updated";
        this.reviewUserEmail = $"updated-{Guid.NewGuid():N}@example.test";
        var editForm = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit User" });
        await editForm.Locator("input:not([type='checkbox'])").First.FillAsync(this.reviewUserName);
        await editForm.Locator("input:not([type='checkbox'])").Nth(1).FillAsync(this.reviewUserEmail);
        await editForm.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
    }

    [Then("the saved details appear and the domain management role remains")]
    public async Task ThenUserDetailsAndRoleRemainAsync()
    {
        await this.SearchUserAsync(this.reviewUserEmail);
        await Assertions.Expect(this.ReviewRow()).ToContainTextAsync(this.reviewUserName);
        var saved = await this.UserFixture.GetUserAsync(this.reviewUserId);
        Assert.NotNull(saved);
        Assert.Equal(SystemRole.DomainManagement, saved.SystemRole);
    }

    [Given("I administer an active user account")]
    public async Task GivenActiveUserAsync()
    {
        await this.PrepareUserAsync();
        this.reviewUserName = $"Active user {Guid.NewGuid():N}";
        (this.reviewUserId, this.reviewUserEmail) = await this.UserFixture.SeedUserAsync(this.reviewUserName,
            lastLoginAt: DateTime.UtcNow.AddDays(-1));
    }

    [When("I suspend the user for an administrative reason")]
    public async Task WhenISuspendUserAsync()
    {
        await this.OpenUsersAsync();
        await this.SearchUserAsync(this.reviewUserEmail);
        await this.ReviewRow().GetByRole(AriaRole.Button, new() { Name = "Suspend", Exact = true }).ClickAsync();
        var suspendDialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Suspend User" });
        await suspendDialog.Locator("select").SelectOptionAsync("Administrative");
        await suspendDialog.GetByRole(AriaRole.Button, new() { Name = "Suspend User", Exact = true }).ClickAsync();
    }

    [Then("the account is shown as suspended")]
    public async Task ThenUserSuspendedAsync()
    {
        await Assertions.Expect(this.ReviewRow()).ToContainTextAsync("Suspended");
        await Assertions.Expect(this.ReviewRow().GetByRole(AriaRole.Button, new() { Name = "Unsuspend" })).ToBeVisibleAsync();
    }

    [When("I restore the user")]
    public async Task WhenIRestoreUserAsync()
    {
        await this.ReviewRow().GetByRole(AriaRole.Button, new() { Name = "Unsuspend" }).ClickAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Unsuspend User", Exact = true }).ClickAsync();
    }

    [Then("the account is shown as active again")]
    public async Task ThenUserActiveAgainAsync()
    {
        await Assertions.Expect(this.ReviewRow()).ToContainTextAsync("Active");
        await Assertions.Expect(this.ReviewRow().GetByRole(AriaRole.Button, new() { Name = "Suspend" })).ToBeVisibleAsync();
    }

    [Given("I administer a user with active and revoked passkeys")]
    public async Task GivenUserWithPasskeysAsync()
    {
        await this.PrepareUserAsync();
        this.reviewUserName = $"Passkey user {Guid.NewGuid():N}";
        (this.reviewUserId, this.reviewUserEmail) = await this.UserFixture.SeedUserAsync(this.reviewUserName);
        await this.UserFixture.SeedPasskeyAsync(this.reviewUserId, "Review active key");
        await this.UserFixture.SeedPasskeyAsync(this.reviewUserId, "Review revoked key", revoked: true);
        var (otherUserId, _) = await this.UserFixture.SeedUserAsync("Other passkey owner");
        await this.UserFixture.SeedPasskeyAsync(otherUserId, "Unrelated key");
    }

    [When("I review that user's passkeys")]
    public async Task WhenIReviewPasskeysAsync()
    {
        await this.OpenUsersAsync();
        await this.SearchUserAsync(this.reviewUserEmail);
        await this.ReviewRow().GetByRole(AriaRole.Button, new() { Name = "Passkeys" }).ClickAsync();
    }

    [Then("only that account's passkeys and their statuses appear")]
    public async Task ThenPasskeysScopedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Review active key")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Review revoked key")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Revoked", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Unrelated key")).ToHaveCountAsync(0);
    }

    [Given("I am signed in without user administration permission")]
    public Task GivenIAmNotUserAdministratorAsync() => this.PrepareUserAsync(administrator: false);

    [When("I navigate to the user management URL")]
    public Task WhenINavigateToUsersAsync() => this.Page.GotoAsync("/admin/users");

    [Then("I cannot view the user list or change an account")]
    public async Task ThenUsersUnavailableAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/access-denied\?ReturnUrl="));
        await Assertions.Expect(this.Page.GetByText("You do not have permission to view this page.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "User Management" })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Add User" })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.Locator("tbody tr")).ToHaveCountAsync(0);
    }
}
