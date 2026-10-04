using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;

namespace Spamma.Browser.Tests;

[Binding]
public sealed class AnonymousAccessSteps
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;
    private IPage? page;

    private IPage Page => this.page ?? throw new InvalidOperationException("Browser scenario has not started.");

    [BeforeScenario]
    public async Task StartBrowserAsync()
    {
        this.playwright = await Playwright.CreateAsync();
        this.browser = await this.playwright.Chromium.LaunchAsync();
        this.context = await this.browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Environment.GetEnvironmentVariable("SPAMMA_E2E_BASE_URL") ?? "http://127.0.0.1:5188",
        });
        await this.context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
        this.page = await this.context.NewPageAsync();
    }

    [AfterScenario]
    public async Task StopBrowserAsync(ScenarioContext scenarioContext)
    {
        if (this.context is not null)
        {
            if (scenarioContext.TestError is not null)
            {
                var outputDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_RESULTS_DIR")
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "browser-test-results");
                Directory.CreateDirectory(outputDirectory);
                var safeName = Regex.Replace(scenarioContext.ScenarioInfo.Title, "[^A-Za-z0-9]+", "-").Trim('-');
                await this.context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = Path.Combine(outputDirectory, $"{safeName}.zip"),
                });
            }
            else
            {
                await this.context.Tracing.StopAsync();
            }

            await this.context.CloseAsync();
        }

        if (this.browser is not null)
        {
            await this.browser.CloseAsync();
        }

        this.playwright?.Dispose();
    }

    [Given("I am visiting Spamma anonymously")]
    public async Task GivenIAmVisitingSpammaAnonymouslyAsync()
    {
        await this.Page.GotoAsync("/");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to Spamma" })).ToBeVisibleAsync();
    }

    [Given("I am on the login page")]
    public async Task GivenIAmOnTheLoginPageAsync()
    {
        await this.Page.GotoAsync("/login");
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeVisibleAsync();
    }

    [Given("I follow a login link without a token")]
    public async Task GivenIFollowALoginLinkWithoutATokenAsync()
    {
        await this.Page.GotoAsync("/logging-in");
    }

    [Given("initial setup has completed")]
    public async Task GivenInitialSetupHasCompletedAsync()
    {
        await this.Page.GotoAsync("/");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Sign In" })).ToBeVisibleAsync();
    }

    [When("I choose to sign in")]
    public async Task WhenIChooseToSignInAsync()
    {
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Sign In" }).ClickAsync();
    }

    [When("I request a magic link for an unregistered address")]
    public async Task WhenIRequestAMagicLinkForAnUnregisteredAddressAsync()
    {
        await this.Page.GetByLabel("Email address").FillAsync("unknown-ui-smoke@example.test");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Send Magic Link" }).ClickAsync();
    }

    [When("I choose to return to login")]
    public async Task WhenIChooseToReturnToLoginAsync()
    {
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Go to Login" }).ClickAsync();
    }

    [When("I try to open the setup login page")]
    public async Task WhenITryToOpenTheSetupLoginPageAsync()
    {
        await this.Page.GotoAsync("/setup-login");
    }

    [When("I try to open the inbox")]
    public async Task WhenITryToOpenTheInboxAsync()
    {
        await this.Page.GotoAsync("/m/inbox");
    }

    [Then("I see the login form")]
    public async Task ThenISeeTheLoginFormAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login(?:\?.*)?$"));
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToBeVisibleAsync();
    }

    [Then("I see a generic check-your-email confirmation")]
    public async Task ThenISeeAGenericCheckYourEmailConfirmationAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Check your email" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("unknown-ui-smoke@example.test")).ToBeVisibleAsync();
    }

    [Then("I am told the link is invalid or expired")]
    public async Task ThenIAmToldTheLinkIsInvalidOrExpiredAsync()
    {
        await Assertions.Expect(this.Page.GetByText("The authentication link is invalid or has expired.")).ToBeVisibleAsync();
    }

    [Then("I am returned to the home page")]
    public async Task ThenIAmReturnedToTheHomePageAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"^https?://[^/]+/$"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Sign In" })).ToBeVisibleAsync();
    }
}
