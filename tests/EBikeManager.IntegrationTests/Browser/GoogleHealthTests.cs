using EBikeManager.TestSupport;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class GoogleHealthTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public GoogleHealthTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task GoogleHealthConnectsWithYourOwnClientAndUploadsRides()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/integrations/google-health/callback?error=access_denied&state=unknown");
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/settings/google-health$"));
        await Expect(page.GetByText("Google reported a problem: access_denied")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await page.GetByRole(AriaRole.Tab, new() { Name = "Google Health" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/settings/google-health$"));
        await Expect(page.GetByText("1. Create your own Google sign-in")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Authorized redirect URI")).ToHaveValueAsync($"{_app.BaseAddress}integrations/google-health/callback");

        var signIn = page.GetByRole(AriaRole.Button, new() { Name = "Sign in with Google" });
        await Expect(signIn).ToBeDisabledAsync();
        await page.GetByLabel("Client ID").FillAsync(FakeGoogleHealthAuth.ClientId);
        await page.GetByLabel("Client secret").FillAsync(FakeGoogleHealthAuth.ClientSecret);
        await page.GetByText("All my rides", new() { Exact = true }).ClickAsync();
        await signIn.ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/settings/google-health$"));
        await Expect(page.GetByText($"Connected to Google Health as {FakeGoogleHealthAuth.Account}.")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Upload now" }).ClickAsync();
        await Expect(page.GetByText("30 uploaded, 0 failed, 0 with a note.", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 30000 });
        await Expect(page.GetByRole(AriaRole.Table)).ToHaveCountAsync(0);
        await Expect(page.Locator(".mud-snackbar")).ToHaveCountAsync(0, new() { Timeout = 30000 });
        await page.EvaluateAsync("window.scrollTo(0, 0)");
        Directory.CreateDirectory(ScreenshotFolder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, "google-health-connected.png"), FullPage = true });

        await page.GotoAsync("/rides");
        await Expect(page.GetByRole(AriaRole.Img, new() { NameRegex = new System.Text.RegularExpressions.Regex("^Uploaded to Google Health") })).ToHaveCountAsync(25);

        await page.GotoAsync("/rides/ride-00");
        await Expect(page.GetByText("Google Health", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Uploaded", new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync("/settings/google-health");
        await page.GetByRole(AriaRole.Button, new() { Name = "Disconnect" }).ClickAsync();
        await Expect(page.GetByText("Google Health is disconnected.", new() { Exact = false })).ToBeVisibleAsync();
        Assert.Contains("google-refresh", _app.GoogleAuth.Revoked);

        AssertNoBrowserErrors();
    }
}
