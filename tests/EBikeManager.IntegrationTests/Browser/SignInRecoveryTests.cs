using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class SignInRecoveryTests : BrowserTest, IClassFixture<BrowserAppSignInSwitchedOff>
{
    private readonly BrowserAppSignInSwitchedOff _app;

    public SignInRecoveryTests(BrowserAppSignInSwitchedOff app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task DisableAuthLetsTheOwnerInToCorrectTheSignInSettings()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides");
        await Expect(page.GetByText("London roundtrip").First).ToBeVisibleAsync();

        await page.GotoAsync("/settings/security");
        await Expect(page.GetByText("Sign-in is switched off by the DISABLE_AUTH environment variable", new() { Exact = false })).ToBeVisibleAsync();
        await page.GetByLabel("Client ID", new() { Exact = true }).FillAsync("ebike-manager-fixed");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(page.GetByText("Sign-in settings saved.")).ToBeVisibleAsync();

        Assert.Equal("ebike-manager-fixed", (await _app.SettingsAsync(TestContext.Current.CancellationToken)).SignIn.ClientId);
        Assert.Empty(_app.Oidc.Authorizations);
        AssertNoBrowserErrors();
    }
}
