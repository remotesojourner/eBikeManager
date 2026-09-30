using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class RidesTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public RidesTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task EveryRideShowsItsFitAndGpxBackup()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides");

        var saved = Row(page, "Ride ride-01");
        await Expect(saved.GetByRole(AriaRole.Button, new() { Name = "Download FIT file" })).ToBeVisibleAsync();
        await Expect(saved.GetByRole(AriaRole.Button, new() { Name = "Download GPX file" })).ToBeVisibleAsync();

        var failed = Row(page, "Ride ride-05");
        await Expect(failed.GetByRole(AriaRole.Img, new() { Name = "FIT: Failed" })).ToBeVisibleAsync();
        await Expect(failed.GetByRole(AriaRole.Button, new() { Name = "Download GPX file" })).ToBeVisibleAsync();

        var missing = Row(page, "Ride ride-04");
        await Expect(missing.GetByRole(AriaRole.Img, new() { Name = "FIT: Not available" })).ToBeVisibleAsync();
        await Expect(missing.GetByRole(AriaRole.Img, new() { Name = "GPX: Not available" })).ToBeVisibleAsync();

        var download = await page.RunAndWaitForDownloadAsync(() => saved.GetByRole(AriaRole.Button, new() { Name = "Download GPX file" }).ClickAsync());
        Assert.EndsWith("_ride-01.gpx", download.SuggestedFilename, StringComparison.Ordinal);
        await Expect(page).ToHaveURLAsync(new Regex("/rides$"));

        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task ClickingAnywhereOnARideOpensIt()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides");
        await Row(page, "Ride ride-02").GetByText("62 %").ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/rides/ride-02$"));
        AssertNoBrowserErrors();
    }

    private static ILocator Row(IPage page, string title) =>
        page.Locator("tr", new() { Has = page.GetByText(title, new() { Exact = true }) });
}
