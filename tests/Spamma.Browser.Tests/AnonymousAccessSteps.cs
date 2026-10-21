using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Playwright;
using Npgsql;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

[Binding]
public sealed partial class AnonymousAccessSteps
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;
    private IPage? page;
    private readonly List<IVideo> additionalVideos = [];

    private IPage Page => this.page ?? throw new InvalidOperationException("Browser scenario has not started.");

    private static string GetScenarioArtifactName(ScenarioContext scenarioContext)
    {
        var values = scenarioContext.ScenarioInfo.Arguments.Values.Cast<object?>()
            .Take(2)
            .Select(value => value?.ToString());
        var name = string.Join("-", new[] { scenarioContext.ScenarioInfo.Title }.Concat(values));
        return Regex.Replace(name, "[^A-Za-z0-9]+", "-").Trim('-');
    }

    [BeforeScenario]
    public async Task StartBrowserAsync(ScenarioContext scenarioContext)
    {
        if (Environment.GetEnvironmentVariable("SPAMMA_E2E_RESET_SETUP_CONFIG") == "true")
        {
            await this.ResetSetupConfigurationAsync();
        }

        this.playwright = await Playwright.CreateAsync();
        this.browser = Environment.GetEnvironmentVariable("SPAMMA_BROWSERSTACK_ENABLED") == "true"
            ? await this.ConnectToBrowserStackAsync(scenarioContext)
            : await this.playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = Environment.GetEnvironmentVariable("SPAMMA_E2E_CHROMIUM_PATH"),
            });
        var videoDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_VIDEO_DIR");
        this.context = await this.browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Environment.GetEnvironmentVariable("SPAMMA_E2E_BASE_URL") ?? "http://127.0.0.1:5188",
            RecordVideoDir = string.IsNullOrWhiteSpace(videoDirectory) ? null : videoDirectory,
            RecordVideoSize = string.IsNullOrWhiteSpace(videoDirectory)
                ? null
                : new RecordVideoSize { Width = 1280, Height = 720 },
        });
        this.context.Page += (_, openedPage) =>
        {
            if (this.page is not null && openedPage.Video is { } popupVideo)
            {
                this.additionalVideos.Add(popupVideo);
            }
        };
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
        if (Environment.GetEnvironmentVariable("SPAMMA_BROWSERSTACK_ENABLED") == "true" && this.page is not null)
        {
            var status = scenarioContext.TestError is null ? "passed" : "failed";
            var reason = scenarioContext.TestError?.Message ?? "Scenario passed";
            var command = JsonSerializer.Serialize(new
            {
                action = "setSessionStatus",
                arguments = new { status, reason },
            });
            await this.page.EvaluateAsync("_ => {}", $"browserstack_executor: {command}");
        }

        if (this.setupSmtpCapture is not null)
        {
            await this.setupSmtpCapture.DisposeAsync();
        }

        if (this.secondSettingsContext is not null)
        {
            var secondVideo = this.secondSettingsPage?.Video;
            await this.secondSettingsContext.CloseAsync();
            var videoDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_VIDEO_DIR");
            if (secondVideo is not null && !string.IsNullOrWhiteSpace(videoDirectory))
            {
                Directory.CreateDirectory(videoDirectory);
                await secondVideo.SaveAsAsync(Path.Combine(videoDirectory,
                    $"{GetScenarioArtifactName(scenarioContext)}-second-user.webm"));
                await secondVideo.DeleteAsync();
            }
        }

        if (this.context is not null)
        {
            if (scenarioContext.TestError is not null)
            {
                var outputDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_RESULTS_DIR")
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "browser-test-results");
                Directory.CreateDirectory(outputDirectory);
                var safeName = GetScenarioArtifactName(scenarioContext);
                await this.context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = Path.Combine(outputDirectory, $"{safeName}.zip"),
                });
            }
            else
            {
                await this.context.Tracing.StopAsync();
            }

            var videoDirectory = Environment.GetEnvironmentVariable("SPAMMA_BROWSER_VIDEO_DIR");
            if (this.page is not null && !string.IsNullOrWhiteSpace(videoDirectory))
            {
                // Give the recorder time to capture the final rendered page after fast redirects.
                await this.page.WaitForTimeoutAsync(750);
            }

            var video = this.page?.Video;
            await this.context.CloseAsync();

            if (video is not null && !string.IsNullOrWhiteSpace(videoDirectory))
            {
                Directory.CreateDirectory(videoDirectory);
                var safeName = GetScenarioArtifactName(scenarioContext);
                await video.SaveAsAsync(Path.Combine(videoDirectory, $"{safeName}.webm"));
                await video.DeleteAsync();
                for (var index = 0; index < this.additionalVideos.Count; index++)
                {
                    var popupVideo = this.additionalVideos[index];
                    if (scenarioContext.ScenarioInfo.Title.Contains("PDF", StringComparison.OrdinalIgnoreCase))
                    {
                        await popupVideo.SaveAsAsync(Path.Combine(videoDirectory, $"{safeName}-print-preview.webm"));
                    }

                    await popupVideo.DeleteAsync();
                }
            }
        }

        if (this.browser is not null)
        {
            await this.browser.CloseAsync();
        }

        this.playwright?.Dispose();
    }

    private async Task<IBrowser> ConnectToBrowserStackAsync(ScenarioContext scenarioContext)
    {
        static string RequiredEnvironmentVariable(string name) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException($"{name} must be set for BrowserStack tests.");

        var playwrightVersion = typeof(IPlaywright).Assembly.GetName().Version?.ToString(3)
            ?? throw new InvalidOperationException("Could not determine the Playwright client version.");
        var capabilities = new Dictionary<string, string>
        {
            ["os"] = "Windows",
            ["os_version"] = "11",
            ["browser"] = "chrome",
            ["browser_version"] = "latest",
            ["browserstack.username"] = RequiredEnvironmentVariable("BROWSERSTACK_USERNAME"),
            ["browserstack.accessKey"] = RequiredEnvironmentVariable("BROWSERSTACK_ACCESS_KEY"),
            ["browserstack.local"] = "true",
            ["browserstack.localIdentifier"] = RequiredEnvironmentVariable("BROWSERSTACK_LOCAL_IDENTIFIER"),
            ["browserstack.video"] = "true",
            ["browserstack.playwrightVersion"] = playwrightVersion,
            ["client.playwrightVersion"] = playwrightVersion,
            ["project"] = Environment.GetEnvironmentVariable("BROWSERSTACK_PROJECT_NAME") ?? "Spamma",
            ["build"] = Environment.GetEnvironmentVariable("BROWSERSTACK_BUILD_NAME") ?? "Spamma browser tests",
            ["name"] = scenarioContext.ScenarioInfo.Title,
        };
        var endpoint = "wss://cdp.browserstack.com/playwright?caps=" +
            Uri.EscapeDataString(JsonSerializer.Serialize(capabilities));
        try
        {
            return await this.playwright!.Chromium.ConnectAsync(endpoint);
        }
        catch (PlaywrightException)
        {
            // Playwright errors can include the endpoint, which contains the access key.
            throw new InvalidOperationException("Could not connect to BrowserStack. Check the credentials and Local tunnel.");
        }
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

    [When("I try to request a magic link with an invalid email address")]
    public async Task WhenITryToRequestAMagicLinkWithAnInvalidEmailAddressAsync()
    {
        await this.Page.GetByLabel("Email address").FillAsync("not-an-email");
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
        await Assertions.Expect(this.Page.GetByText("If an account exists for unknown-ui-smoke@example.test, we'll send a magic link.")).ToBeVisibleAsync();
    }

    [Then("I stay on the login form without a confirmation")]
    public async Task ThenIStayOnTheLoginFormWithoutAConfirmationAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login(?:\?.*)?$"));
        await Assertions.Expect(this.Page.GetByLabel("Email address")).ToHaveValueAsync("not-an-email");
        Assert.False(await this.Page.GetByLabel("Email address").EvaluateAsync<bool>("element => element.checkValidity()"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Check your email" })).ToHaveCountAsync(0);
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
