using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace Spamma.Browser.Tests;

public sealed partial class AnonymousAccessSteps
{
    private IResponse? accessDeniedResponse;

    [When("I open the access denied page directly")]
    public async Task WhenIOpenAccessDeniedDirectlyAsync() =>
        this.accessDeniedResponse = await this.Page.GotoAsync("/access-denied");

    [Then("I see a branded permission explanation and administrator guidance")]
    public async Task ThenISeeHelpfulAccessDeniedAsync()
    {
        Assert.NotNull(this.accessDeniedResponse);
        Assert.Equal(200, this.accessDeniedResponse.Status);
        Assert.Contains("text/html", this.accessDeniedResponse.Headers["content-type"]);
        await Assertions.Expect(this.Page).ToHaveTitleAsync("Access denied | Spamma");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Main)).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" })).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("Your account does not have permission to view this page.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByText("If you need access, contact your Spamma administrator.")).ToBeVisibleAsync();
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Return to inbox" })).ToHaveAttributeAsync("href", "/m/inbox");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Go back" })).ToHaveAttributeAsync("href", "/m/inbox");
        await this.Page.WaitForTimeoutAsync(700);
    }

    [Then("I can return to my inbox")]
    public async Task ThenICanReturnToInboxAsync()
    {
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Return to inbox" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Inbox" })).ToBeVisibleAsync();
    }

    [When("I open access denied with an external ReturnUrl")]
    public async Task WhenIOpenAccessDeniedWithExternalReturnUrlAsync()
    {
        await this.Page.GotoAsync("/m/campaigns");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Campaigns" })).ToBeVisibleAsync();
        var previousPage = this.Page.Url;
        var returnUrl = Uri.EscapeDataString("https://outside.example/restricted");
        await this.Page.GotoAsync($"/access-denied?ReturnUrl={returnUrl}",
            new PageGotoOptions { Referer = previousPage });
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/access-denied\?ReturnUrl="));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" })).ToBeVisibleAsync();
    }

    [Then("recovery actions stay inside Spamma")]
    public async Task ThenRecoveryActionsAreLocalAsync()
    {
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Return to inbox" }))
            .ToHaveAttributeAsync("href", "/m/inbox");
        await Assertions.Expect(this.Page.Locator("a[href^='https://outside.example']")).ToHaveCountAsync(0);
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Go back" })).ToHaveAttributeAsync("href", "/m/campaigns");
        await this.Page.WaitForTimeoutAsync(700);
    }

    [Then("Go back returns to the page I came from")]
    public async Task ThenGoBackReturnsToPriorPageAsync()
    {
        await this.Page.GetByRole(AriaRole.Link, new() { Name = "Go back" }).ClickAsync();
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/campaigns$"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Campaigns" })).ToBeVisibleAsync();
    }

    [When("I open access denied at mobile width")]
    public async Task WhenIOpenAccessDeniedOnMobileAsync()
    {
        await this.Page.SetViewportSizeAsync(390, 700);
        await this.Page.GotoAsync("/access-denied");
    }

    [Then("the page fits the viewport and both actions are keyboard operable")]
    public async Task ThenMobileAccessDeniedIsKeyboardOperableAsync()
    {
        Assert.True(await this.Page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth <= window.innerWidth"));
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" })).ToBeVisibleAsync();
        await this.Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Spamma inbox" })).ToBeFocusedAsync();
        await this.Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Return to inbox" })).ToBeFocusedAsync();
        await this.Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(this.Page.GetByRole(AriaRole.Link, new() { Name = "Go back" })).ToBeFocusedAsync();
        await this.Page.WaitForTimeoutAsync(700);
        await this.Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(this.Page).ToHaveURLAsync(new Regex(@"/m/inbox$"));
    }
}
