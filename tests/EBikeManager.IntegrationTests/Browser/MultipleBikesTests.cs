using System.Text.RegularExpressions;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class MultipleBikesTests : BrowserTest, IClassFixture<BrowserAppWithTwoBikes>
{
    private const string BikeName = "TENWAYS (Performance Line)";

    private readonly BrowserAppWithTwoBikes _app;

    public MultipleBikesTests(BrowserAppWithTwoBikes app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheBikesPageHasATabPerBikeAndTheDashboardFlipsBetweenThem()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/bikes");
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = BrowserAppWithTwoBikes.OtherBikeName })).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = BrowserAppWithTwoBikes.OtherBikeName })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Tab, new() { Name = BikeName }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex($@"/bikes\?bike={SampleData.BikeId}$"));
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = BikeName })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = BrowserAppWithTwoBikes.OtherBikeName })).ToHaveCountAsync(0);

        await page.GotoAsync("/");
        var carousel = page.GetByRole(AriaRole.Region, new() { Name = "Bikes" });
        await Expect(carousel.GetByText(BrowserAppWithTwoBikes.OtherBikeName, new() { Exact = true })).ToBeVisibleAsync();
        await carousel.GetByRole(AriaRole.Button, new() { Name = "Next bike" }).ClickAsync();
        await Expect(carousel.GetByText(BikeName, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(carousel.GetByText(BrowserAppWithTwoBikes.OtherBikeName, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(carousel.GetByRole(AriaRole.Button, new() { Name = $"Show {BikeName}" })).ToHaveAttributeAsync("aria-current", "true");
        await carousel.GetByRole(AriaRole.Button, new() { Name = "Next bike" }).ClickAsync();
        await Expect(carousel.GetByText(BrowserAppWithTwoBikes.OtherBikeName, new() { Exact = true })).ToBeVisibleAsync();
        await carousel.GetByRole(AriaRole.Button, new() { Name = $"Show {BikeName}" }).ClickAsync();
        await carousel.GetByRole(AriaRole.Link, new() { Name = "Bike details" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex($@"/bikes\?bike={SampleData.BikeId}$"));
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = BikeName })).ToHaveAttributeAsync("aria-selected", "true");
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task TheRidesListNamesEachRidesBikeAndFiltersByBike()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        var otherRide = page.GetByRole(AriaRole.Link, new() { Name = BrowserAppWithTwoBikes.OtherBikeRide });
        var bikeRide = page.GetByRole(AriaRole.Link, new() { Name = "Ride ride-01" });

        await page.GotoAsync("/rides");
        await Expect(Row(page, BrowserAppWithTwoBikes.OtherBikeRide).GetByText($"· {BrowserAppWithTwoBikes.OtherBikeName}")).ToBeVisibleAsync();
        await Expect(Row(page, "Ride ride-01").GetByText($"· {BikeName}")).ToBeVisibleAsync();

        await page.GetByLabel("Bike", new() { Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = BrowserAppWithTwoBikes.OtherBikeName }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex($@"/rides\?bike={SampleData.OtherBikeId}$"));
        await Expect(otherRide).ToBeVisibleAsync();
        await Expect(bikeRide).ToHaveCountAsync(0);

        await otherRide.ClickAsync();
        await Expect(page.GetByText($"· {BrowserAppWithTwoBikes.OtherBikeName}")).ToBeVisibleAsync();
        await page.GoBackAsync();
        await Expect(page).ToHaveURLAsync(new Regex($@"/rides\?bike={SampleData.OtherBikeId}$"));
        await Expect(otherRide).ToBeVisibleAsync();
        await Expect(bikeRide).ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }

    private static ILocator Row(IPage page, string title) =>
        page.Locator("tr", new() { Has = page.GetByText(title, new() { Exact = true }) });
}
