using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Npgsql;
using Reqnroll;

namespace Spamma.Browser.Tests;

[Binding]
public sealed partial class AnonymousAccessSteps
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;
    private IPage? page;

    private IPage Page => this.page ?? throw new InvalidOperationException("Browser scenario has not started.");

    [BeforeScenario]
    public async Task StartBrowserAsync()
    {
        if (Environment.GetEnvironmentVariable("SPAMMA_E2E_RESET_SETUP_CONFIG") == "true")
        {
            await this.ResetSetupConfigurationAsync();
        }

        this.playwright = await Playwright.CreateAsync();
        this.browser = await this.playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            ExecutablePath = Environment.GetEnvironmentVariable("SPAMMA_E2E_CHROMIUM_PATH"),
        });
        var videoDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_VIDEO_DIR");
        this.context = await this.browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Environment.GetEnvironmentVariable("SPAMMA_E2E_BASE_URL") ?? "http://127.0.0.1:5188",
            RecordVideoDir = videoDirectory,
            RecordVideoSize = string.IsNullOrWhiteSpace(videoDirectory)
                ? null
                : new RecordVideoSize { Width = 1280, Height = 720 },
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

            var video = this.page?.Video;
            await this.context.CloseAsync();

            var videoDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_VIDEO_DIR");
            if (video is not null && !string.IsNullOrWhiteSpace(videoDirectory))
            {
                Directory.CreateDirectory(videoDirectory);
                var safeName = Regex.Replace(scenarioContext.ScenarioInfo.Title, "[^A-Za-z0-9]+", "-").Trim('-');
                await video.SaveAsAsync(Path.Combine(videoDirectory, $"{safeName}.webm"));
                await video.DeleteAsync();
            }
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

    [Given("Spamma is in setup mode")]
    public async Task GivenSpammaIsInSetupModeAsync()
    {
        await this.Page.GotoAsync("/setup-login");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Spamma Setup Access" })).ToBeVisibleAsync();
    }

    [When("I open the application for setup")]
    public async Task WhenIOpenTheApplicationForSetupAsync() => await this.Page.GotoAsync("/");

    [When("I open a setup step without a session")]
    public async Task WhenIOpenASetupStepWithoutASessionAsync() => await this.Page.GotoAsync("/setup/keys");

    [Then("I am asked for the setup password")]
    public async Task ThenIAmAskedForTheSetupPasswordAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/setup-login$"));
        await Assertions.Expect(this.Page.GetByLabel("Setup Password")).ToBeVisibleAsync();
    }

    [When("I enter the valid setup password")]
    public async Task WhenIEnterTheValidSetupPasswordAsync()
    {
        var password = Environment.GetEnvironmentVariable("SPAMMA_SETUP_PASSWORD")
            ?? throw new InvalidOperationException("SPAMMA_SETUP_PASSWORD must be set for setup browser tests.");
        await this.Page.GetByLabel("Setup Password").FillAsync(password);
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Access Setup Wizard" }).ClickAsync();
    }

    [Then("I am shown the setup welcome page")]
    public async Task ThenIAmShownTheSetupWelcomePageAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/setup$"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to Spamma Setup" })).ToBeVisibleAsync();
    }

    [Then("I can proceed to security keys")]
    public async Task ThenICanProceedToSecurityKeysAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Get Started" })).ToHaveAttributeAsync("href", "/setup/keys");
    }

    [When("I enter an incorrect setup password")]
    public async Task WhenIEnterAnIncorrectSetupPasswordAsync()
    {
        await this.Page.GetByLabel("Setup Password").FillAsync("incorrect-setup-password");
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Access Setup Wizard" }).ClickAsync();
    }

    [Then("I remain outside the setup wizard")]
    public async Task ThenIRemainOutsideTheSetupWizardAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Invalid setup password.", new() { Exact = false })).ToBeVisibleAsync();
        await this.Page.GotoAsync("/setup/keys");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/setup-login$"));
    }

    [Given("I have entered the setup wizard")]
    public async Task GivenIHaveEnteredTheSetupWizardAsync()
    {
        await this.Page.GotoAsync("/setup-login");
        await this.WhenIEnterTheValidSetupPasswordAsync();
        await this.ThenIAmShownTheSetupWelcomePageAsync();
    }

    [Given("security keys, outbound email and an administrator are configured")]
    public async Task GivenOtherRequiredSetupStepsAreConfiguredAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("The setup test database connection must be configured.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var (key, value) in new[]
        {
            ("security.signingKey", "test-only-signing-key"),
            ("smtp.host", "smtp.example.test"),
            ("from.email", "setup@example.test"),
            ("primaryuser.id", Guid.NewGuid().ToString()),
        })
        {
            await using var command = new NpgsqlCommand(
                "INSERT INTO app_configuration (key, value) VALUES (@key, @value) " +
                "ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value", connection);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("value", value);
            await command.ExecuteNonQueryAsync();
        }
    }

    [When("I open the setup completion page")]
    public async Task WhenIOpenTheSetupCompletionPageAsync() => await this.Page.GotoAsync("/setup/complete");

    [Then("I see that hosting configuration is missing")]
    public async Task ThenISeeThatHostingConfigurationIsMissingAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Setup Incomplete" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Hosting Configuration Missing", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("a[href='/setup/hosting']")).ToBeVisibleAsync();
    }

    [Then("I cannot finalize setup")]
    public async Task ThenICannotFinalizeSetupAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Button, new() { Name = "Complete Setup & Go to Login" })).ToHaveCountAsync(0);
    }
}
