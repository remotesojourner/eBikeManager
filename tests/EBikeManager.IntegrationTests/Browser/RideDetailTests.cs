using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class RideDetailTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public RideDetailTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task ARideOpensWithItsMapChartsAndSplits()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides");
        await page.GetByRole(AriaRole.Link, new() { Name = "London roundtrip" }).First.ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/rides/ride-00$"));
        await Expect(page.GetByText("London roundtrip").First).ToBeVisibleAsync();
        await Expect(page.Locator(".em-map .maplibregl-canvas")).ToBeVisibleAsync();
        await Expect(page.Locator(".em-ride-chart .uplot")).ToHaveCountAsync(4);
        await Expect(page.Locator(".mud-table-body .mud-table-row")).ToHaveCountAsync(13);
        await Expect(page.GetByText("You did 62% of the work")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Download GPX file" })).ToBeVisibleAsync();

        await page.SetViewportSizeAsync(DesktopWidth, 2400);
        await page.Locator(".em-ride-chart[data-series='speed'] .u-over").HoverAsync();
        await Expect(page.Locator(".em-readout")).ToContainTextAsync("km/h");
        Directory.CreateDirectory(ScreenshotFolder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, "ride-detail-hover.png") });

        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task ARideWithoutAFitFileExplainsWhyThereIsNoMap()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides/ride-04");

        await Expect(page.GetByText("Bosch has no FIT file for this ride")).ToBeVisibleAsync();
        await Expect(page.Locator(".maplibregl-canvas")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Download FIT file" })).ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task AnUnknownRideSaysSo()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides/no-such-ride");

        await Expect(page.GetByText("That ride doesn't exist.")).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }
}
