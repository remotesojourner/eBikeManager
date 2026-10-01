using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class BoschSettingsTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public BoschSettingsTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheSavedBikesShowWithoutAskingBoschUntilRefreshFindsAnotherBike()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        var saved = page.GetByRole(AriaRole.Checkbox, new() { Name = "TENWAYS (Performance Line)" });
        var other = page.GetByRole(AriaRole.Checkbox, new() { Name = "Cube (Performance Line CX)" });

        await page.GotoAsync("/settings/bosch");
        await Expect(saved).ToBeCheckedAsync();
        await Expect(other).ToHaveCountAsync(0);
        Assert.Equal(0, _app.Bosch.BikeListReads);

        await page.GetByRole(AriaRole.Button, new() { Name = "Refresh bikes" }).ClickAsync();
        await Expect(page.GetByText("Bikes read from your Bosch account.")).ToBeVisibleAsync();
        await Expect(other).Not.ToBeCheckedAsync();
        await Expect(saved).ToBeCheckedAsync();
        Assert.Equal(1, _app.Bosch.BikeListReads);

        await other.CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save bikes" }).ClickAsync();
        await Expect(page.GetByText("Bikes saved.")).ToBeVisibleAsync();

        Assert.Equal([SampleData.BikeId, SampleData.OtherBikeId], (await _app.BikesAsync(cancellationToken)).Select(bike => bike.Id).Order());
        Assert.True((await _app.SettingsAsync(cancellationToken)).Bosch.FullScanRequested);
        AssertNoBrowserErrors();
    }
}
