using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class PageLayoutTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private static readonly (string Path, string Name, string Landmark)[] _pages =
    [
        ("/", "dashboard", "Recent rides"),
        ("/rides", "rides", "FIT backup"),
        ("/settings/bosch", "bosch", "Bosch eBike Flow account"),
        ("/settings/schedule", "schedule", "Sync schedule"),
        ("/settings/security", "security", "Require this password"),
        ("/settings/about", "about", "Built with"),
        ("/welcome", "welcome", "Welcome to eBike Manager!")
    ];

    private readonly BrowserAppWithRides _app;

    public PageLayoutTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    public static TheoryData<BrowserTheme, int> Views => new()
    {
        { BrowserTheme.Dark, DesktopWidth },
        { BrowserTheme.Light, DesktopWidth },
        { BrowserTheme.Dark, PhoneWidth },
        { BrowserTheme.Light, PhoneWidth }
    };

    [Theory]
    [MemberData(nameof(Views))]
    public async Task EveryPageRendersWholeWithoutScrollingSideways(BrowserTheme theme, int width)
    {
        await using var browser = await OpenBrowserAsync(_app, theme, width);
        var page = await browser.NewPageAsync();
        Directory.CreateDirectory(ScreenshotFolder);
        var problems = new List<string>();

        foreach (var (path, name, landmark) in _pages)
        {
            await page.GotoAsync(path);
            await Expect(page.GetByText(landmark).First).ToBeVisibleAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, $"{name}-{theme.ToString().ToLowerInvariant()}-{width}.png"), FullPage = true });

            if (await ShowsAnErrorAsync(page)) problems.Add($"{path} shows an error");
            if (await ScrollsSidewaysAsync(page)) problems.Add($"{path} scrolls sideways");
        }

        Assert.Empty(problems);
        AssertNoBrowserErrors();
    }
}
