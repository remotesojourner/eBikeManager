using EBikeManager.Application.BackgroundServices;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class BridgeTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public BridgeTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheBridgeShowsTheBatteryAlwaysAndLiveValuesWhileTheBikeIsConnected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var key = FakeEsphomeBridge.NewKey();
        await using var bridge = new FakeEsphomeBridge(Convert.FromBase64String(key));
        await bridge.SetAsync(FakeEsphomeBridge.Battery, 50f);
        await bridge.SetAsync(FakeEsphomeBridge.Odometer, 41.6f);
        await bridge.SetAsync(FakeEsphomeBridge.Speed, 23.4f);
        await bridge.SetAsync(FakeEsphomeBridge.Connected, true);
        var listener = ActivatorUtilities.CreateInstance<BridgeListenerService>(_app.Services);
        await listener.StartAsync(cancellationToken);
        try
        {
            await using var browser = await OpenBrowserAsync(_app);
            var page = await browser.NewPageAsync();

            await page.GotoAsync("/settings/bridge");
            await Expect(page.GetByText("Not in use. Enter the bridge's address to connect.")).ToBeVisibleAsync();
            await Expect(page.GetByRole(AriaRole.Link, new() { Name = "What you need and how to flash the bridge" })).ToHaveAttributeAsync("href", "https://xunil99.github.io/ha-bosch-ebike/");
            await page.GetByLabel("Address", new() { Exact = true }).FillAsync(bridge.Address);
            await page.GetByLabel("Encryption key", new() { Exact = true }).FillAsync(key);
            await page.GetByRole(AriaRole.Button, new() { Name = "Save bridge" }).ClickAsync();
            await Expect(page.GetByText($"Connected to {FakeEsphomeBridge.FriendlyName} at {bridge.Address}, ESPHome {FakeEsphomeBridge.EsphomeVersion}.")).ToBeVisibleAsync();
            await Expect(page.GetByText("The bridge reads 41.6 km on this bike's odometer.")).ToBeVisibleAsync();

            await page.GotoAsync("/bikes");
            var live = page.Locator(".mud-card", new() { Has = page.GetByText("Live from the bridge") });
            await Expect(live.GetByText("23.4 km/h")).ToBeVisibleAsync();
            await Expect(page.GetByText("42", new() { Exact = true })).ToBeVisibleAsync();

            await bridge.SetAsync(FakeEsphomeBridge.Connected, false);
            await Expect(live).ToHaveCountAsync(0);
            await Expect(page.GetByText(new System.Text.RegularExpressions.Regex(@"^50 % \(read "))).ToBeVisibleAsync();
            AssertNoBrowserErrors();
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
        }
    }
}
