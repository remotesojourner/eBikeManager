using System.Text.RegularExpressions;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class BikeTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public BikeTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheDashboardShowsTheBikeAndLeadsToItsPage()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/");
        var card = page.Locator(".mud-card", new() { Has = page.GetByText("TENWAYS (Performance Line)", new() { Exact = true }) });
        await Expect(card.GetByText("36 km", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(card.GetByText("0.9 charge cycles")).ToBeVisibleAsync();
        await card.GetByRole(AriaRole.Link, new() { Name = "Bike details" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex($@"/bikes\?bike={SampleData.BikeId}$"));
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task TheBikePageShowsTheMileagePerModeAndTheBikePassDocuments()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/bikes");

        await Expect(page.GetByText("Seat stem", new() { Exact = true })).ToBeVisibleAsync();
        var mileage = page.Locator(".em-mileage");
        await Expect(mileage).ToContainTextAsync("SPORT");
        await Expect(mileage).ToContainTextAsync("18.6 km");
        await Expect(mileage).ToContainTextAsync("51%");
        await Expect(mileage).ToContainTextAsync("142 Wh");
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "ECO 0.0 km, AUTO 14.0 km, SPORT 18.6 km, TURBO 3.8 km" })).ToBeVisibleAsync();

        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Bike photo" }).Locator("img")).ToBeVisibleAsync();
        var invoice = page.GetByRole(AriaRole.Link, new() { Name = "Bike invoice" });
        var address = await invoice.GetAttributeAsync("href");
        Assert.StartsWith($"bikes/{SampleData.BikeId}/documents/{BoschSamples.InvoiceFileId}?v=", address);
        var response = await page.APIRequest.GetAsync(address!);
        Assert.Equal(200, response.Status);
        Assert.Equal("application/pdf", response.Headers["content-type"]);

        AssertNoBrowserErrors();
    }
}
