using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class SignInTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private const string NewPassword = "correct horse battery";

    private readonly BrowserAppWithRides _app;

    public SignInTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task SettingAPasswordKeepsTheOwnerSignedInAndLocksEveryoneElseOut()
    {
        await using var owner = await OpenBrowserAsync(_app);
        var page = await owner.NewPageAsync();
        await page.GotoAsync("/settings/security");
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync(NewPassword);
        await page.GetByLabel("Confirm password").FillAsync(NewPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Require this password" }).ClickAsync();
        await Expect(page.GetByText("A password is required to use this app.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Sign out" })).ToBeVisibleAsync();

        await using var visitor = await OpenBrowserAsync(_app, BrowserTheme.Light);
        var other = await visitor.NewPageAsync();
        await other.GotoAsync("/rides");
        await Expect(other.GetByText("This eBike Manager is protected with a password.")).ToBeVisibleAsync();
        await Expect(other.GetByText("Edinburgh roundtrip")).ToHaveCountAsync(0);
        await other.GetByLabel("Password").FillAsync("wrong password");
        await other.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Expect(other.GetByText("That password isn't right.")).ToBeVisibleAsync();
        await other.GetByLabel("Password").FillAsync(NewPassword);
        await other.GetByLabel("Password").PressAsync("Enter");
        await Expect(other.GetByText("Edinburgh roundtrip").First).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Stop asking for a password" }).ClickAsync();
        await Expect(page.GetByText("The password is no longer required.")).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }
}
