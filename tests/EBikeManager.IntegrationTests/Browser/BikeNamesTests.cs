using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class BikeNamesTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private const string Model = "TENWAYS (Performance Line)";

    private readonly BrowserAppWithRides _app;

    public BikeNamesTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task ABikeCanBeRenamedWhileItsModelStaysTheOneFromBosch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        var name = page.GetByLabel("Name", new() { Exact = true });
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save names" });

        await page.GotoAsync("/settings/bosch");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = Model })).ToBeVisibleAsync();
        await Expect(name).ToHaveValueAsync(Model);
        await Expect(save).ToBeDisabledAsync();

        await name.FillAsync("");
        await Expect(page.GetByText("Enter a name.")).ToBeVisibleAsync();
        await Expect(save).ToBeDisabledAsync();

        await name.FillAsync("Commuter");
        await save.ClickAsync();
        await Expect(page.GetByText("Names saved.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = $"Commuter · {Model}" })).ToBeCheckedAsync();

        var bike = Assert.Single(await _app.BikesAsync(cancellationToken));
        Assert.Equal(("Commuter", Model), (bike.Name, bike.Model));
        Assert.All(await _app.RidesAsync(cancellationToken), ride => Assert.Equal(("Commuter", Model), (ride.BikeName, ride.BikeModel)));

        await page.GotoAsync("/bikes");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Commuter" })).ToBeVisibleAsync();
        await Expect(page.GetByText(Model, new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync("/rides/ride-01");
        await Expect(page.GetByText($"· Commuter · {Model}")).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }
}
