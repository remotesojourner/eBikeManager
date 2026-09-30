using EBikeManager.Application.Configuration;
using EBikeManager.TestSupport;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class WelcomeTests : BrowserTest, IClassFixture<BrowserAppNotSetUp>
{
    private readonly BrowserAppNotSetUp _app;

    public WelcomeTests(BrowserAppNotSetUp app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheWizardConnectsBoschChoosesBikesAndStartsTheFirstSync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/");
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/welcome$"));
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to eBike Manager!" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Continue" })).ToBeDisabledAsync();
        var loginUrl = await page.GetByRole(AriaRole.Link, new() { Name = "Open Bosch login" }).GetAttributeAsync("href");
        await page.GetByLabel("The copied oauth2redirect URL").FillAsync("not the url");
        await page.GetByRole(AriaRole.Button, new() { Name = "Connect" }).ClickAsync();
        await Expect(page.GetByText("doesn't look like the oauth2redirect URL")).ToBeVisibleAsync();

        loginUrl = await page.GetByRole(AriaRole.Link, new() { Name = "Open Bosch login" }).GetAttributeAsync("href");
        await page.GetByLabel("The copied oauth2redirect URL").FillAsync(FakeBoschAuth.RedirectFor(loginUrl!));
        await page.GetByRole(AriaRole.Button, new() { Name = "Connect" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Choose your bikes" })).ToBeVisibleAsync();
        await Expect(page.GetByLabel("TENWAYS (Performance Line)")).ToBeCheckedAsync();
        await page.GetByLabel("Cube (Performance Line CX)").UncheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "How often should rides sync?" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Every hour Recommended" })).ToHaveAttributeAsync("aria-pressed", "true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Finish" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/rides$"));
        await Expect(page.GetByText("Edinburgh roundtrip").First).ToBeVisibleAsync();

        var settings = await _app.SettingsAsync(cancellationToken);
        Assert.True(settings.SetupCompleted);
        Assert.Equal(ScheduleSettings.Hourly, settings.Schedule.Cron);
        Assert.Equal(FakeBoschAuth.Account, settings.Bosch.AccountName);
        AssertNoBrowserErrors();
    }
}
