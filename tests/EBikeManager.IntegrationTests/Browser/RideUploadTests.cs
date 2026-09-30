using EBikeManager.Application.Configuration;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class RideUploadTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public RideUploadTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task SpecificRidesCanBeUploadedToGoogleHealthFromTheRidesAndRidePages()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using (var scope = _app.Services.CreateScope())
        {
            await SampleData.ConnectGoogleHealthAsync(scope.ServiceProvider, GoogleHealthSettings.UploadFromValue(DateTime.UtcNow), cancellationToken);
        }

        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/rides");
        var row = page.Locator("tr", new() { Has = page.GetByText("Ride ride-05", new() { Exact = true }) });
        await row.GetByRole(AriaRole.Button, new() { Name = "Upload to Google Health" }).ClickAsync();
        await Expect(page.GetByText("Uploaded Ride ride-05 to Google Health.")).ToBeVisibleAsync();
        await Expect(row.GetByRole(AriaRole.Img, new() { NameRegex = new System.Text.RegularExpressions.Regex("^Uploaded to Google Health") })).ToBeVisibleAsync();
        await Expect(row.GetByRole(AriaRole.Button, new() { Name = "Upload to Google Health" })).ToHaveCountAsync(0);
        Directory.CreateDirectory(ScreenshotFolder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, "rides-google-health.png") });

        await page.GotoAsync("/rides/ride-07");
        var upload = page.GetByRole(AriaRole.Button, new() { Name = "Upload to Google Health", Exact = true });
        await upload.ClickAsync();
        await Expect(page.GetByText("Uploaded Ride ride-07 to Google Health.")).ToBeVisibleAsync();
        await Expect(page.GetByText("Google Health", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(upload).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Upload to Google Health again" })).ToBeVisibleAsync();

        Assert.Equal(
            [FakeGoogleHealthApi.DataPointFor("ride-05"), FakeGoogleHealthApi.DataPointFor("ride-07")],
            _app.Google.Uploads.Select(uploaded => uploaded.DataPointId));
        AssertNoBrowserErrors();
    }
}
