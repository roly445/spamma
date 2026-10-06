using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private DomainScenarioFixture? credentialFixture;
    private string credentialKeyName = string.Empty;
    private string credentialKeyValue = string.Empty;
    private string credentialPasskeyName = string.Empty;
    private string otherKeyName = string.Empty;
    private string otherPasskeyName = string.Empty;

    private DomainScenarioFixture CredentialFixture => this.credentialFixture
        ?? throw new InvalidOperationException("The credential fixture has not been created.");

    private ILocator ApiKeyCard(string name) => this.Page.Locator("div.space-y-4 > div")
        .Filter(new() { Has = this.Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }) });

    private ILocator PasskeyCard(string name) => this.Page.Locator("div.space-y-4 > div")
        .Filter(new() { Has = this.Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }) });

    [AfterScenario("credential")]
    public async Task DisposeCredentialFixtureAsync()
    {
        if (this.credentialFixture is not null) await this.credentialFixture.DisposeAsync();
    }

    private async Task SignInForCredentialsAsync(Func<Task>? seed = null)
    {
        this.credentialFixture = await DomainScenarioFixture.CreateAsync(false);
        if (seed is not null) await seed();
        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync(this.CredentialFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await this.Page.GotoAsync(await this.CredentialFixture.WaitForLoginPathAsync());
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    private async Task OpenApiKeysAsync()
    {
        await this.Page.GotoAsync("/account/api-keys");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "API Keys", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Loading API keys...")).ToHaveCountAsync(0);
    }

    private async Task OpenPasskeysAsync()
    {
        await this.Page.GotoAsync("/account/passkeys");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Passkeys", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Loading your passkeys...")).ToHaveCountAsync(0);
    }

    private async Task CreateApiKeyAsync(string name)
    {
        await this.OpenApiKeysAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Create API Key" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Create API Key" });
        await dialog.GetByLabel("Key Name").FillAsync(name);
        await dialog.GetByLabel("Expiry").SelectOptionAsync("30");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "API Key Created Successfully" })).ToBeVisibleAsync();
        this.credentialKeyValue = (await this.Page.Locator(".font-mono").InnerTextAsync()).Trim();
        Assert.StartsWith("sk-", this.credentialKeyValue);
        await Assertions.Expect(this.ApiKeyCard(name)).ToBeVisibleAsync();
    }

    private async Task RegisterPasskeyAsync(string name)
    {
        await this.OpenPasskeysAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Passkey" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Name Your Passkey" });
        await dialog.GetByPlaceholder("e.g., My YubiKey").FillAsync(name);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Register Passkey" }).ClickAsync();
        await Assertions.Expect(this.PasskeyCard(name)).ToBeVisibleAsync();
    }

    [Given("my browser supports passkeys")]
    public async Task GivenBrowserSupportsPasskeysAsync()
    {
        var cdp = await this.Page.Context.NewCDPSessionAsync(this.Page);
        await cdp.SendAsync("WebAuthn.enable");
        await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new Dictionary<string, object>
            {
                ["protocol"] = "ctap2", ["transport"] = "internal", ["hasResidentKey"] = true,
                ["hasUserVerification"] = true, ["isUserVerified"] = true, ["automaticPresenceSimulation"] = true,
            },
        });
    }

    [Given("I am signed in to manage credentials")]
    public Task GivenSignedInForCredentialsAsync() => this.SignInForCredentialsAsync();

    [When("I create an API key with a name and expiry")]
    public async Task WhenCreateApiKeyAsync()
    {
        this.credentialKeyName = $"Review key {Guid.NewGuid():N}"[..24];
        await this.CreateApiKeyAsync(this.credentialKeyName);
    }

    [Then("I can copy the key value immediately")]
    public async Task ThenCanCopyApiKeyAsync()
    {
        await this.Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Copy", Exact = true }).ClickAsync();
        var copied = await this.Page.EvaluateAsync<string>("navigator.clipboard.readText()");
        Assert.Equal(this.credentialKeyValue, copied);
        await Assertions.Expect(this.ApiKeyCard(this.credentialKeyName)).ToContainTextAsync("Expires");
    }

    [Then("the key value is not shown again after I leave the page")]
    public async Task ThenApiKeyIsHiddenAfterLeavingAsync()
    {
        await this.OpenPasskeysAsync();
        await this.OpenApiKeysAsync();
        await Assertions.Expect(this.ApiKeyCard(this.credentialKeyName)).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText(this.credentialKeyValue, new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Copy", Exact = true })).ToHaveCountAsync(0);
    }

    [Given("my account has active, expired, and revoked API keys")]
    public Task GivenApiKeysWithStatusesAsync() => this.SignInForCredentialsAsync(async () =>
    {
        await this.CredentialFixture.SeedApiKeyAsync(this.CredentialFixture.UserId, "Active review key", DateTimeOffset.UtcNow.AddDays(30));
        await this.CredentialFixture.SeedApiKeyAsync(this.CredentialFixture.UserId, "Expired review key", DateTimeOffset.UtcNow.AddDays(-1));
        await this.CredentialFixture.SeedApiKeyAsync(this.CredentialFixture.UserId, "Revoked review key", revoked: true);
    });

    [When("I select each API key status filter")]
    public async Task WhenFilterApiKeysAsync()
    {
        await this.OpenApiKeysAsync();
        foreach (var (filter, visible) in new[]
        {
            ("Active", "Active review key"), ("Expired", "Expired review key"), ("Revoked", "Revoked review key"),
        })
        {
            await this.Page.GetByRole(AriaRole.Button, new() { Name = filter, Exact = true }).ClickAsync();
            await Assertions.Expect(this.ApiKeyCard(visible)).ToBeVisibleAsync();
            foreach (var hidden in new[] { "Active review key", "Expired review key", "Revoked review key" }.Where(x => x != visible))
                await Assertions.Expect(this.ApiKeyCard(hidden)).ToHaveCountAsync(0);
            await this.Page.WaitForTimeoutAsync(550);
        }
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "All", Exact = true }).ClickAsync();
    }

    [Then("I see only my API keys with that status")]
    public async Task ThenApiKeyFiltersWorkAsync()
    {
        foreach (var name in new[] { "Active review key", "Expired review key", "Revoked review key" })
            await Assertions.Expect(this.ApiKeyCard(name)).ToBeVisibleAsync();
    }

    [Given("my account has an active API key")]
    public Task GivenActiveApiKeyAsync() => this.SignInForCredentialsAsync();

    [When("I confirm that I want to revoke it")]
    public async Task WhenRevokeApiKeyAsync()
    {
        this.credentialKeyName = $"Revoke key {Guid.NewGuid():N}"[..24];
        await this.CreateApiKeyAsync(this.credentialKeyName);
        await this.ApiKeyCard(this.credentialKeyName).GetByRole(AriaRole.Button, new() { Name = "Revoke" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Revoke API Key" });
        await Assertions.Expect(dialog).ToContainTextAsync(this.credentialKeyName);
        await Assertions.Expect(this.ApiKeyCard(this.credentialKeyName)).ToContainTextAsync("Active");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Revoke Key" }).ClickAsync();
    }

    [Then("it is shown as revoked")]
    public Task ThenApiKeyRevokedAsync() => Assertions.Expect(this.ApiKeyCard(this.credentialKeyName)).ToContainTextAsync("Revoked");

    [Then("the key is no longer active")]
    public async Task ThenApiKeyNoLongerActiveAsync()
    {
        var key = await this.CredentialFixture.GetApiKeyAsync(this.credentialKeyName);
        Assert.NotNull(key);
        Assert.False(key.IsActive);
        await Assertions.Expect(this.ApiKeyCard(this.credentialKeyName).GetByRole(AriaRole.Button, new() { Name = "Revoke" })).ToHaveCountAsync(0);
    }

    [When("I name a new passkey and complete browser registration")]
    public async Task WhenRegisterPasskeyAsync()
    {
        this.credentialPasskeyName = $"Review passkey {Guid.NewGuid():N}"[..27];
        await this.RegisterPasskeyAsync(this.credentialPasskeyName);
    }

    [Then("the passkey appears in my account")]
    public async Task ThenPasskeyAppearsAsync()
    {
        await Assertions.Expect(this.PasskeyCard(this.credentialPasskeyName)).ToContainTextAsync("Active");
        Assert.NotNull(await this.CredentialFixture.GetPasskeyAsync(this.credentialPasskeyName));
    }

    [When("I cancel passkey registration")]
    public async Task WhenCancelPasskeyRegistrationAsync()
    {
        await this.OpenPasskeysAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Passkey" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Name Your Passkey" });
        await dialog.GetByPlaceholder("e.g., My YubiKey").FillAsync("Cancelled review passkey");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
    }

    [Then("no new passkey appears in my account")]
    public async Task ThenNoPasskeyAppearsAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Name Your Passkey" })).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByText("No passkeys")).ToBeVisibleAsync();
        Assert.Null(await this.CredentialFixture.GetPasskeyAsync("Cancelled review passkey"));
    }

    [Given("my account has active and revoked passkeys")]
    public Task GivenPasskeysWithStatusesAsync() => this.SignInForCredentialsAsync(async () =>
    {
        await this.CredentialFixture.SeedPasskeyAsync(this.CredentialFixture.UserId, "Active review passkey");
        await this.CredentialFixture.SeedPasskeyAsync(this.CredentialFixture.UserId, "Revoked review passkey", revoked: true);
    });

    [When("I select each passkey status filter")]
    public async Task WhenFilterPasskeysAsync()
    {
        await this.OpenPasskeysAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Active", Exact = true }).ClickAsync();
        await Assertions.Expect(this.PasskeyCard("Active review passkey")).ToBeVisibleAsync();
        await Assertions.Expect(this.PasskeyCard("Revoked review passkey")).ToHaveCountAsync(0);
        await this.Page.WaitForTimeoutAsync(550);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Revoked", Exact = true }).ClickAsync();
        await Assertions.Expect(this.PasskeyCard("Revoked review passkey")).ToBeVisibleAsync();
        await Assertions.Expect(this.PasskeyCard("Active review passkey")).ToHaveCountAsync(0);
        await this.Page.WaitForTimeoutAsync(550);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "All", Exact = true }).ClickAsync();
    }

    [Then("I see only my passkeys with that status")]
    public async Task ThenPasskeyFiltersWorkAsync()
    {
        await Assertions.Expect(this.PasskeyCard("Active review passkey")).ToBeVisibleAsync();
        await Assertions.Expect(this.PasskeyCard("Revoked review passkey")).ToBeVisibleAsync();
    }

    [Given("I have registered a passkey")]
    public async Task GivenRegisteredPasskeyAsync()
    {
        this.credentialPasskeyName = $"Revoke passkey {Guid.NewGuid():N}"[..27];
        await this.RegisterPasskeyAsync(this.credentialPasskeyName);
    }

    [When("I revoke that passkey")]
    public Task WhenRevokePasskeyAsync() => this.PasskeyCard(this.credentialPasskeyName)
        .GetByRole(AriaRole.Button, new() { Name = "Revoke" }).ClickAsync();

    [Then("it is shown as a revoked passkey")]
    public async Task ThenPasskeyRevokedAsync()
    {
        await Assertions.Expect(this.PasskeyCard(this.credentialPasskeyName)).ToContainTextAsync("Revoked");
        var passkey = await this.CredentialFixture.GetPasskeyAsync(this.credentialPasskeyName);
        Assert.NotNull(passkey);
        Assert.True(passkey.IsRevoked);
    }

    [Then("it cannot be used for a later login")]
    public async Task ThenRevokedPasskeyCannotLoginAsync()
    {
        await this.Page.GotoAsync("/logout");
        await this.Page.GotoAsync("/login");
        await this.Page.Locator("#passkeyLoginBtn").ClickAsync();
        await Assertions.Expect(this.Page.Locator("#passkeyErrorContainer")).ToContainTextAsync("Authentication failed");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login(?:\?.*)?$"));
    }

    [Given("another user has API keys and passkeys")]
    public Task GivenAnotherUserHasCredentialsAsync() => this.SignInForCredentialsAsync(async () =>
    {
        var other = await this.CredentialFixture.SeedUserAsync("Other credential user");
        this.otherKeyName = $"Other key {Guid.NewGuid():N}"[..24];
        this.otherPasskeyName = $"Other passkey {Guid.NewGuid():N}"[..27];
        await this.CredentialFixture.SeedApiKeyAsync(other.Id, this.otherKeyName);
        await this.CredentialFixture.SeedPasskeyAsync(other.Id, this.otherPasskeyName);
        await this.CredentialFixture.SeedApiKeyAsync(this.CredentialFixture.UserId, "My review key");
        await this.CredentialFixture.SeedPasskeyAsync(this.CredentialFixture.UserId, "My review passkey");
    });

    [When("I open my credential pages")]
    public async Task WhenOpenCredentialPagesAsync()
    {
        await this.OpenApiKeysAsync();
        await Assertions.Expect(this.ApiKeyCard("My review key")).ToBeVisibleAsync();
        await Assertions.Expect(this.ApiKeyCard(this.otherKeyName)).ToHaveCountAsync(0);
        await this.Page.WaitForTimeoutAsync(600);
        await this.OpenPasskeysAsync();
    }

    [Then("I see only credentials belonging to my account")]
    public async Task ThenOnlyOwnCredentialsAsync()
    {
        await Assertions.Expect(this.PasskeyCard("My review passkey")).ToBeVisibleAsync();
        await Assertions.Expect(this.PasskeyCard(this.otherPasskeyName)).ToHaveCountAsync(0);
    }
}
