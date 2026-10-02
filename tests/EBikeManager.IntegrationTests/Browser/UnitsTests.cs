using EBikeManager.Application.Enums;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class UnitsTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public UnitsTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task ImperialShowsMilesMphAndFeetEverywhere()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/settings/display");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Metric" })).ToHaveAttributeAsync("aria-pressed", "true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Imperial" }).ClickAsync();
        await Expect(page.GetByText("Units saved.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Imperial" })).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.Equal(UnitSystem.Imperial, (await _app.SettingsAsync(cancellationToken)).Units);

        await page.GotoAsync("/");
        await Expect(page.GetByText("12.4 mi").First).ToBeVisibleAsync();
        await Expect(page.GetByText("23 mi", new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync("/rides");
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "12.4 mi" }).First).ToBeVisibleAsync();

        await page.GotoAsync("/rides/ride-00");
        await Expect(page.GetByText("Every mile of the ride, timed while moving.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Mile" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Speed · mph")).ToBeVisibleAsync();
        await Expect(page.GetByText("Elevation · ft")).ToBeVisibleAsync();
        await Expect(page.GetByText("Top speed 20.4 mph")).ToBeVisibleAsync();
        await Expect(page.GetByText("394 ft up, 387 ft down")).ToBeVisibleAsync();
        await Expect(page.Locator(".mud-table-body .mud-table-row")).ToHaveCountAsync(8);
        await page.SetViewportSizeAsync(DesktopWidth, 2400);
        await page.Locator(".em-ride-chart[data-series='speed'] .u-over").HoverAsync();
        await Expect(page.Locator(".em-readout")).ToContainTextAsync("mph");
        await Expect(page.Locator(".em-readout")).ToContainTextAsync("mi Distance");
        Directory.CreateDirectory(ScreenshotFolder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, "ride-imperial.png") });

        await page.GotoAsync("/bikes");
        await Expect(page.GetByText("Distance per assistance mode")).ToBeVisibleAsync();
        await Expect(page.GetByText("17 mph", new() { Exact = true })).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, "bike-imperial.png"), FullPage = true });

        await page.GotoAsync("/settings/display");
        await page.GetByRole(AriaRole.Button, new() { Name = "Metric" }).ClickAsync();
        await Expect(page.GetByText("Units saved.")).ToBeVisibleAsync();
        await page.GotoAsync("/rides");
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "20.0 km" }).First).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }
}
