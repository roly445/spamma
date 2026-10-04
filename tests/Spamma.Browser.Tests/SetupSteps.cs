using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Npgsql;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private const string TestMailHostname = "mail.example.test";
    private bool certificateRequestSeen;

    private static string SetupConnectionString => Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? throw new InvalidOperationException("The setup test database connection must be configured.");

    private async Task ResetSetupConfigurationAsync()
    {
        await using var connection = new NpgsqlConnection(SetupConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM app_configuration", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetConfigurationAsync(string key, string value)
    {
        await using var connection = new NpgsqlConnection(SetupConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO app_configuration (key, value) VALUES (@key, @value) " +
            "ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value", connection);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("value", value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> GetConfigurationAsync(string key)
    {
        await using var connection = new NpgsqlConnection(SetupConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT value FROM app_configuration WHERE key = @key", connection);
        command.Parameters.AddWithValue("key", key);
        return (string?)await command.ExecuteScalarAsync();
    }

    private static async Task SeedRequiredConfigurationAsync(bool includeAdmin)
    {
        await SetConfigurationAsync("security.signingKey", "test-only-signing-key");
        await SetConfigurationAsync("application.baseUrl", "https://mail.example.test");
        await SetConfigurationAsync("application.mailServerHostname", TestMailHostname);
        await SetConfigurationAsync("application.mxPriority", "10");
        await SetConfigurationAsync("smtp.host", "smtp.example.test");
        await SetConfigurationAsync("from.email", "setup@example.test");
        if (includeAdmin)
        {
            await SetConfigurationAsync("primaryuser.id", Guid.NewGuid().ToString());
        }
    }

    [Given("I am on the security keys setup step")]
    public async Task GivenIAmOnTheSecurityKeysSetupStepAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await this.Page.GotoAsync("/setup/keys");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Generate Security Keys" })).ToBeVisibleAsync();
    }

    [When("I generate and save the keys")]
    public async Task WhenIGenerateAndSaveTheKeysAsync() => await this.GenerateAndSaveKeysAsync();

    private async Task GenerateAndSaveKeysAsync()
    {
        var area = await this.Page.Locator("#entropy-area").BoundingBoxAsync()
            ?? throw new InvalidOperationException("Entropy collection area is not visible.");
        await this.Page.Mouse.MoveAsync(area.X + 10, area.Y + 10);
        await this.Page.Mouse.MoveAsync(area.X + area.Width - 10, area.Y + area.Height - 10, new() { Steps = 120 });
        await Assertions.Expect(this.Page.Locator("#generate-btn")).ToBeEnabledAsync();
        await this.Page.Locator("#generate-btn").ClickAsync();
    }

    [Then("the keys step is complete")]
    public async Task ThenTheKeysStepIsCompleteAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Security keys saved successfully!", new() { Exact = true })).ToBeVisibleAsync();
        Assert.False(string.IsNullOrWhiteSpace(await GetConfigurationAsync("security.signingKey")));
    }

    [Then("I can proceed to hosting configuration")]
    public async Task ThenICanProceedToHostingConfigurationAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Continue to Hosting Configuration" })).ToHaveAttributeAsync("href", "/setup/hosting");
    }

    [Given("security keys have already been saved")]
    public async Task GivenSecurityKeysHaveAlreadyBeenSavedAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await SetConfigurationAsync("security.signingKey", "original-test-key");
        await this.Page.GotoAsync("/setup/keys");
        await Assertions.Expect(this.Page.GetByText("Existing Security Keys Found")).ToBeVisibleAsync();
    }

    [When("I ask to regenerate them")]
    public async Task WhenIAskToRegenerateThemAsync() => await this.Page.Locator("#regenerate-keys-btn").ClickAsync();

    [Then("I must confirm regeneration before the saved key changes")]
    public async Task ThenIAmAskedToConfirmKeyRegenerationAsync()
    {
        await Assertions.Expect(this.Page.Locator("#regeneration-warning")).ToBeVisibleAsync();
        Assert.Equal("original-test-key", await GetConfigurationAsync("security.signingKey"));
        await this.Page.Locator("#cancel-regeneration-btn").ClickAsync();
        await Assertions.Expect(this.Page.Locator("#regeneration-warning")).ToBeHiddenAsync();
        Assert.Equal("original-test-key", await GetConfigurationAsync("security.signingKey"));

        await this.Page.Locator("#regenerate-keys-btn").ClickAsync();
        await this.Page.Locator("#proceed-regeneration-btn").ClickAsync();
        await Assertions.Expect(this.Page.Locator("#entropy-area")).ToBeVisibleAsync();
        await this.GenerateAndSaveKeysAsync();
        await Assertions.Expect(this.Page.GetByText("Security keys saved successfully!", new() { Exact = true })).ToBeVisibleAsync();
        Assert.NotEqual("original-test-key", await GetConfigurationAsync("security.signingKey"));
    }

    [Given("I am on the hosting setup step")]
    public async Task GivenIAmOnTheHostingSetupStepAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await this.Page.GotoAsync("/setup/hosting");
    }

    [When("I provide valid server and email routing settings")]
    public async Task WhenIProvideValidHostingSettingsAsync()
    {
        await this.Page.Locator("#base-url").FillAsync("https://mail.example.test");
        await this.Page.Locator("#mail-server-hostname").FillAsync(TestMailHostname);
        await this.Page.Locator("#mx-priority").FillAsync("20");
        await this.Page.Locator("button[type=submit]").ClickAsync();
    }

    [Then("the hosting configuration is saved")]
    public async Task ThenTheHostingConfigurationIsSavedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Hosting configuration saved successfully!", new() { Exact = true })).ToBeVisibleAsync();
        Assert.Equal("https://mail.example.test", await GetConfigurationAsync("application.baseUrl"));
        Assert.Equal(TestMailHostname, await GetConfigurationAsync("application.mailServerHostname"));
        Assert.Equal("20", await GetConfigurationAsync("application.mxPriority"));
    }

    [Then("I can proceed to email configuration")]
    public async Task ThenICanProceedToEmailConfigurationAsync() =>
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Continue to Email Configuration" })).ToHaveAttributeAsync("href", "/setup/email");

    [Given("I am on the email setup step")]
    public async Task GivenIAmOnTheEmailSetupStepAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await this.Page.GotoAsync("/setup/email");
    }

    [When("I provide valid SMTP settings")]
    public async Task WhenIProvideValidSmtpSettingsAsync()
    {
        await this.Page.Locator("#from-email").FillAsync("sender@example.test");
        await this.Page.Locator("#from-name").FillAsync("Spamma Tests");
        await this.Page.Locator("#smtp-host").FillAsync("smtp.example.test");
        await this.Page.Locator("#smtp-port").FillAsync("2525");
        await this.Page.Locator("#email-configuration-form button[type=submit]").ClickAsync();
    }

    [Then("the email configuration is saved")]
    public async Task ThenTheEmailConfigurationIsSavedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Email configuration saved successfully!", new() { Exact = true })).ToBeVisibleAsync();
        Assert.Equal("smtp.example.test", await GetConfigurationAsync("smtp.host"));
        Assert.Equal("2525", await GetConfigurationAsync("smtp.port"));
        Assert.Equal("sender@example.test", await GetConfigurationAsync("from.email"));
    }

    [Then("I can proceed to certificate configuration")]
    public async Task ThenICanProceedToCertificateConfigurationAsync() =>
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Continue to SSL Certificates" })).ToHaveAttributeAsync("href", "/setup/certificates");

    [When("I choose an available email provider preset")]
    public async Task WhenIChooseAnEmailPresetAsync() => await this.Page.Locator("[data-preset=gmail]").ClickAsync();

    [Then("its SMTP settings populate the form")]
    public async Task ThenThePresetPopulatesTheFormAsync()
    {
        await Assertions.Expect(this.Page.Locator("#smtp-host")).ToHaveValueAsync("smtp.gmail.com");
        await Assertions.Expect(this.Page.Locator("#smtp-port")).ToHaveValueAsync("587");
        await Assertions.Expect(this.Page.Locator("#use-ssl")).ToBeCheckedAsync();
    }

    [Then("I can edit them before saving")]
    public async Task ThenICanEditPresetSettingsAsync()
    {
        await this.Page.Locator("#smtp-host").FillAsync("custom.example.test");
        await Assertions.Expect(this.Page.Locator("#smtp-host")).ToHaveValueAsync("custom.example.test");
    }

    [Given("I am on the certificate setup step")]
    public async Task GivenIAmOnTheCertificateSetupStepAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await SetConfigurationAsync("application.mailServerHostname", TestMailHostname);
        await this.Page.GotoAsync("/setup/certificates");
    }

    [When("I choose to skip certificate generation")]
    public async Task WhenIChooseToSkipCertificatesAsync() => await this.Page.Locator("#action-buttons a[href='/setup/admin']").ClickAsync();

    [Then("I can proceed to administrator creation")]
    [Then("I proceed to administrator creation")]
    public async Task ThenIReachAdministratorCreationAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/setup/admin$"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Create Admin User" })).ToBeVisibleAsync();
    }

    [Given("the test ACME service will reject the request")]
    public async Task GivenAcmeWillRejectAsync() => await this.MockAcmeAsync("data: error:Test ACME rejection\n\n");

    [Given("the test ACME service will accept the request")]
    public async Task GivenAcmeWillAcceptAsync() => await this.MockAcmeAsync("data: complete:certificate ready\n\n");

    private async Task MockAcmeAsync(string response)
    {
        await this.Page.RouteAsync("**/api/setup/generate-certificate-stream", async route =>
        {
            this.certificateRequestSeen = route.Request.Method == "POST" &&
                route.Request.PostData?.Contains(TestMailHostname, StringComparison.Ordinal) == true &&
                route.Request.PostData.Contains("admin@example.test", StringComparison.Ordinal);
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/event-stream", Body = response });
        });
    }

    [When("I select Let's Encrypt and request a certificate")]
    public async Task WhenIRequestCertificateAsync()
    {
        await this.Page.Locator("#opt-letsencrypt").CheckAsync();
        await Assertions.Expect(this.Page.Locator("#letsencrypt-config")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.Locator("#domain")).ToHaveValueAsync(TestMailHostname);
        await this.Page.Locator("#email").FillAsync("admin@example.test");
        await this.Page.Locator("#generate-btn").ClickAsync();
    }

    [Then("the certificate failure is reported to me")]
    public async Task ThenCertificateFailureIsReportedAsync()
    {
        await Assertions.Expect(this.Page.Locator("#error-text")).ToHaveTextAsync("Test ACME rejection");
        Assert.True(this.certificateRequestSeen);
    }

    [Given("the required setup settings have been saved")]
    public async Task GivenRequiredSetupSettingsHaveBeenSavedAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await SeedRequiredConfigurationAsync(includeAdmin: false);
        await this.Page.GotoAsync("/setup/admin");
    }

    [When("I create the initial administrator")]
    public async Task WhenICreateTheInitialAdministratorAsync()
    {
        await this.Page.Locator("#admin-name").FillAsync("Setup Administrator");
        await this.Page.Locator("#admin-email").FillAsync($"setup-{Guid.NewGuid():N}@example.test");
        await this.Page.Locator("#admin-form-section button[type=submit]").ClickAsync();
    }

    [Then("the administrator account is recorded")]
    public async Task ThenTheAdministratorIsRecordedAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Admin user created successfully!", new() { Exact = true })).ToBeVisibleAsync();
        Assert.True(Guid.TryParse(await GetConfigurationAsync("primaryuser.id"), out var id) && id != Guid.Empty);
    }

    [Then("I can review setup completion")]
    public async Task ThenICanReviewSetupCompletionAsync() =>
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Complete Setup" })).ToHaveAttributeAsync("href", "/setup/complete");

    [Given("an administrator has already been created")]
    public async Task GivenAnAdministratorAlreadyExistsAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await SeedRequiredConfigurationAsync(includeAdmin: true);
    }

    [When("I open the administrator setup step")]
    public async Task WhenIOpenTheAdministratorSetupStepAsync() => await this.Page.GotoAsync("/setup/admin");

    [Then("I can skip to setup completion")]
    public async Task ThenICanSkipToCompletionAsync()
    {
        await Assertions.Expect(this.Page.GetByText("Admin User Already Exists")).ToBeVisibleAsync();
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Skip - Complete Setup" }).ClickAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Spamma Setup Complete" })).ToBeVisibleAsync();
    }

    [Given("every required setup setting is present")]
    public async Task GivenEveryRequiredSetupSettingIsPresentAsync()
    {
        await this.GivenIHaveEnteredTheSetupWizardAsync();
        await SeedRequiredConfigurationAsync(includeAdmin: true);
        await this.Page.GotoAsync("/setup/complete");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Spamma Setup Complete" })).ToBeVisibleAsync();
    }

    [When("I finalize setup")]
    public async Task WhenIFinalizeSetupAsync() =>
        await this.Page.GetByRole(AriaRole.Button, new() { Name = "Complete Setup & Go to Login" }).ClickAsync();

    [Then("the application leaves setup mode")]
    public async Task ThenTheApplicationLeavesSetupModeAsync()
    {
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/login$"));
        Assert.False(string.IsNullOrWhiteSpace(await GetConfigurationAsync("setup.completed")));
    }

    [Then("setup pages are no longer available to ordinary visitors")]
    public async Task ThenSetupPagesAreUnavailableAsync()
    {
        await this.Page.GotoAsync("/setup-login");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"^https?://[^/]+/$"));
    }
}
