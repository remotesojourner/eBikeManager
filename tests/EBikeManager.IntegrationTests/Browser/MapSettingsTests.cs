using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class MapSettingsTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private const string OwnStyle = "https://maps.example.test/styles/liberty";

    private readonly BrowserAppWithRides _app;

    public MapSettingsTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheMapCanComeFromOpenFreeMapOrYourOwnServer()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save map" });

        await page.GotoAsync("/settings/map");
        await Expect(page.Locator(".em-map .maplibregl-canvas")).ToBeVisibleAsync();
        await Expect(save).ToBeDisabledAsync();

        await page.GetByText("OpenFreeMap", new() { Exact = true }).ClickAsync();
        await save.ClickAsync();
        await Expect(page.GetByText("Map saved.")).ToBeVisibleAsync();
        Assert.Equal(MapProvider.OpenFreeMap, (await _app.SettingsAsync(cancellationToken)).Map.Provider);

        await page.GetByText("Your own map server").ClickAsync();
        var styleUrl = page.GetByLabel("Style URL", new() { Exact = true });
        await styleUrl.FillAsync("maps.example.test");
        await styleUrl.BlurAsync();
        await Expect(page.GetByText("Enter the full address, starting with https:// or http://")).ToBeVisibleAsync();
        await Expect(save).ToBeDisabledAsync();

        await styleUrl.FillAsync(OwnStyle);
        await styleUrl.BlurAsync();
        await Expect(save).ToBeEnabledAsync();
        await save.ClickAsync();
        await Expect(page.GetByText("Map saved.").Last).ToBeVisibleAsync();

        Assert.Equal(new MapSettings(MapProvider.Custom, OwnStyle, null), (await _app.SettingsAsync(cancellationToken)).Map);
        AssertNoBrowserErrors();
    }
}
