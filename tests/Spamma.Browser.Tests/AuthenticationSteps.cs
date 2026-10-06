using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private AuthenticationScenarioFixture? authenticationFixture;
    private string authenticationLoginPath = string.Empty;

    private AuthenticationScenarioFixture AuthenticationFixture => this.authenticationFixture
        ?? throw new InvalidOperationException("The authentication fixture has not been created.");

    [AfterScenario("authentication")]
    public async Task DisposeAuthenticationFixtureAsync()
    {
        if (this.authenticationFixture is not null)
        {
            await this.authenticationFixture.DisposeAsync();
        }
    }

    private async Task PrepareAuthenticationAccountAsync(bool suspended = false)
    {
        this.authenticationFixture = await AuthenticationScenarioFixture.CreateAsync(suspended);
        await this.Page.GotoAsync("/login");
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeVisibleAsync();
    }

    private async Task RequestAuthenticationLinkAsync()
    {
        await this.Page.GetByLabel("Email address").FillAsync(this.AuthenticationFixture.EmailAddress);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Check your email" })).ToBeVisibleAsync();
    }

    private async Task ReceiveAuthenticationLinkAsync()
    {
        await this.RequestAuthenticationLinkAsync();
        this.authenticationLoginPath = await this.AuthenticationFixture.WaitForLoginPathAsync();
    }

    private async Task ConfirmAuthenticationLinkAsync()
    {
        await this.Page.GotoAsync(this.authenticationLoginPath);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
    }

    private async Task EnableAuthenticationPasskeyAsync()
    {
        var cdp = await this.Page.Context.NewCDPSessionAsync(this.Page);
        await cdp.SendAsync("WebAuthn.enable");
        await cdp.SendAsync("WebAuthn.addVirtualAuthenticator",
            new Dictionary<string, object>
            {
                ["options"] = new Dictionary<string, object>
                {
                    ["protocol"] = "ctap2",
                    ["transport"] = "internal",
                    ["hasResidentKey"] = true,
                    ["hasUserVerification"] = true,
                    ["isUserVerified"] = true,
                    ["automaticPresenceSimulation"] = true,
                },
            });
    }

    private async Task PrepareRegisteredAuthenticationPasskeyAsync()
    {
        await this.PrepareAuthenticationAccountAsync();
        await this.EnableAuthenticationPasskeyAsync();
        await this.ReceiveAuthenticationLinkAsync();
        await this.ConfirmAuthenticationLinkAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
        await this.Page.GotoAsync("/account/passkeys");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Passkeys", Exact = true })).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Add Passkey" }).ClickAsync();
        var dialog = this.Page.GetByRole(AriaRole.Dialog, new() { Name = "Name Your Passkey" });
        await dialog.GetByPlaceholder("e.g., My YubiKey").FillAsync("Authentication review passkey");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Register Passkey" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Authentication review passkey" })).ToBeVisibleAsync();
        await this.Page.GotoAsync("/logout");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "You've been logged out" })).ToBeVisibleAsync();
    }

    [Given("I have an active account")]
    public Task GivenActiveAuthenticationAccountAsync() => this.PrepareAuthenticationAccountAsync();

    [Given("my account has been suspended")]
    public Task GivenSuspendedAuthenticationAccountAsync() => this.PrepareAuthenticationAccountAsync(suspended: true);

    [When("I request a magic link for my email address")]
    public Task WhenRequestAuthenticationLinkAsync() => this.RequestAuthenticationLinkAsync();

    [Then("I see the same conditional check-your-email confirmation shown for an unregistered address")]
    public async Task ThenRegisteredConfirmationIsGenericAsync()
    {
        await Assertions.Expect(this.Page.GetByText(
            $"If an account exists for {this.AuthenticationFixture.EmailAddress}, we'll send a magic link.")).ToBeVisibleAsync();
        await this.Page.GotoAsync("/login");
        await this.Page.GetByLabel("Email address").FillAsync("unregistered-auth-review@example.test");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText(
            "If an account exists for unregistered-auth-review@example.test, we'll send a magic link.")).ToBeVisibleAsync();
    }

    [Then("one login message containing a confirmation link is sent to my address")]
    public async Task ThenOneLoginMessageIsSentAsync()
    {
        this.authenticationLoginPath = await this.AuthenticationFixture.WaitForLoginPathAsync();
        Assert.Equal(1, this.AuthenticationFixture.CapturedMessageCount);
        Assert.StartsWith("/logging-in?token=", this.authenticationLoginPath);
    }

    [Then("I see the conditional check-your-email confirmation")]
    public async Task ThenSuspendedConfirmationIsGenericAsync() =>
        await Assertions.Expect(this.Page.GetByText(
            $"If an account exists for {this.AuthenticationFixture.EmailAddress}, we'll send a magic link.")).ToBeVisibleAsync();

    [Then("no login message is sent to my address")]
    public async Task ThenNoLoginMessageIsSentAsync()
    {
        Assert.Equal(0, await this.AuthenticationFixture.CountStartedAttemptsAsync());
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.Equal(0, this.AuthenticationFixture.CapturedMessageCount);
    }

    [Given("I received an unused magic link while my account was active")]
    [Given("I have received an unused magic link")]
    public async Task GivenUnusedAuthenticationLinkAsync()
    {
        await this.PrepareAuthenticationAccountAsync();
        await this.ReceiveAuthenticationLinkAsync();
    }

    [Given("my account has since been suspended")]
    public Task GivenAccountSuspendedAfterLinkAsync() => this.AuthenticationFixture.SuspendAsync();

    [When("I try to confirm the login using that link")]
    [When("I open the link and try to confirm the login")]
    public Task WhenConfirmInvalidAuthenticationLinkAsync() => this.ConfirmAuthenticationLinkAsync();

    [Then("I cannot open my inbox")]
    [Then("I cannot open my inbox in that session")]
    public async Task ThenAuthenticationInboxIsBlockedAsync()
    {
        await this.Page.GotoAsync("/m/inbox");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login(?:\?.*)?$"));
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeVisibleAsync();
    }

    [When("I open the link and select Continue to Spamma")]
    public Task WhenConfirmValidAuthenticationLinkAsync() => this.ConfirmAuthenticationLinkAsync();

    [Then("I am signed in to my account")]
    public async Task ThenAuthenticationSucceededAsync() =>
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));

    [Then("I can open my inbox")]
    public async Task ThenAuthenticationInboxOpensAsync()
    {
        await this.Page.GotoAsync("/m/inbox");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
    }

    [Given("I have a magic link whose 15-minute lifetime has passed")]
    public async Task GivenExpiredAuthenticationLinkAsync()
    {
        await this.PrepareAuthenticationAccountAsync();
        this.authenticationLoginPath = await this.AuthenticationFixture.SeedExpiredLoginPathAsync();
    }

    [Then("I can return to the login page")]
    public async Task ThenCanReturnToAuthenticationLoginAsync()
    {
        await this.Page.GotoAsync(this.authenticationLoginPath);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByText("The authentication link is invalid or has expired.")).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Go to Login" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeVisibleAsync();
    }

    [Given("I have already signed in using a magic link")]
    public async Task GivenAuthenticationLinkWasUsedAsync()
    {
        await this.PrepareAuthenticationAccountAsync();
        await this.ReceiveAuthenticationLinkAsync();
        await this.ConfirmAuthenticationLinkAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    [When("I open the same link and try to confirm the login again in a new browser session")]
    public async Task WhenReuseAuthenticationLinkAsync()
    {
        await this.Page.Context.ClearCookiesAsync();
        Assert.DoesNotContain(await this.Page.Context.CookiesAsync(),
            cookie => cookie.Name.Contains("AspNetCore.Cookies", StringComparison.Ordinal));
        await this.Page.GotoAsync(this.authenticationLoginPath);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Spamma" }).ClickAsync();
    }

    [Given("my account has an active passkey")]
    public Task GivenActiveAuthenticationPasskeyAsync() => this.PrepareRegisteredAuthenticationPasskeyAsync();

    [When("I choose passkey login and complete the browser challenge")]
    public async Task WhenAuthenticateWithPasskeyAsync()
    {
        await this.Page.GotoAsync("/login");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Sign in with Security Key" }).ClickAsync();
    }

    [When("I cancel or fail the browser challenge")]
    public async Task WhenAuthenticationPasskeyChallengeFailsAsync()
    {
        await this.Page.AddInitScriptAsync("""
            Object.defineProperty(navigator.credentials, 'get', {
                configurable: true,
                value: async options => {
                    if (options?.publicKey) throw new DOMException('Cancelled', 'NotAllowedError');
                    return null;
                }
            });
            """);
        await this.Page.GotoAsync("/login");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Sign in with Security Key" }).ClickAsync();
    }

    [Then("I remain signed out")]
    public async Task ThenAuthenticationRemainsSignedOutAsync()
    {
        await Assertions.Expect(this.Page.Locator("#passkeyErrorContainer"))
            .ToContainTextAsync("Authentication failed");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login(?:\?.*)?$"));
    }

    [Then("I can use another login method")]
    public async Task ThenAlternativeAuthenticationAvailableAsync()
    {
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeEnabledAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" })).ToBeEnabledAsync();
    }

    [Given("I am signed in")]
    public async Task GivenSignedInForAuthenticationAsync()
    {
        await this.PrepareAuthenticationAccountAsync();
        await this.ReceiveAuthenticationLinkAsync();
        await this.ConfirmAuthenticationLinkAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }

    [When("I log out")]
    public Task WhenLogOutAuthenticationAsync() => this.Page.GotoAsync("/logout");

    [Then("I see the logout confirmation")]
    public async Task ThenAuthenticationLogoutConfirmedAsync() =>
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "You've been logged out" })).ToBeVisibleAsync();

    [Then("I cannot reopen the inbox without signing in again")]
    public async Task ThenLoggedOutInboxBlockedAsync()
    {
        await this.Page.GotoAsync("/m/inbox");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login(?:\?.*)?$"));
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeVisibleAsync();
    }
}
