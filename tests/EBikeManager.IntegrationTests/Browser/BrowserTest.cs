using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace EBikeManager.IntegrationTests.Browser;

public abstract class BrowserTest
{
    public const int DesktopWidth = 1280;
    public const int PhoneWidth = 390;

    private const string MapTiles = "https://tile.openstreetmap.org/";
    private const string MapStyles = "/styles/";
    private const string BlankStyle = """{"version":8,"sources":{},"layers":[{"id":"background","type":"background","paint":{"background-color":"#dfe3e8"}}]}""";

    private static readonly byte[] _blankTile = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGO4//gFAAVPAqtspm6mAAAAAElFTkSuQmCC");

    private readonly Chromium _chromium;
    private readonly ConcurrentQueue<string> _browserErrors = new();

    static BrowserTest()
    {
        Assertions.SetDefaultExpectTimeout(15000);
    }

    protected BrowserTest(Chromium chromium)
    {
        _chromium = chromium;
    }

    protected static string ScreenshotFolder { get; } = Path.Combine(AppContext.BaseDirectory, "TestResults", "browser");

    protected async Task<IBrowserContext> OpenBrowserAsync(BrowserApp app, BrowserTheme theme = BrowserTheme.Dark, int width = DesktopWidth)
    {
        if (_chromium.Browser is not { } browser)
        {
            Assert.Skip(_chromium.Unavailable);
            throw new InvalidOperationException(_chromium.Unavailable);
        }

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = app.BaseAddress.ToString(),
            ViewportSize = new ViewportSize { Width = width, Height = width == PhoneWidth ? 844 : 900 },
            TimezoneId = "Europe/London",
            Locale = "en-GB"
        });
        context.SetDefaultTimeout(15000);
        var appAddress = app.BaseAddress.ToString();
        await context.RouteAsync(url => !url.StartsWith(appAddress, StringComparison.Ordinal), AnswerOutsideRequestAsync);
        await context.AddInitScriptAsync($"if (location.origin !== 'null') localStorage.setItem('theme', '{theme.ToString().ToLowerInvariant()}')");
        context.Page += (_, page) => Watch(page);
        return context;
    }

    protected void AssertNoBrowserErrors() => Assert.True(_browserErrors.IsEmpty, string.Join(Environment.NewLine, _browserErrors));

    protected static async Task<bool> ShowsAnErrorAsync(IPage page) =>
        await page.GetByText("Something went wrong").CountAsync() > 0 || await page.Locator("#blazor-error-ui").IsVisibleAsync();

    protected static Task<bool> ScrollsSidewaysAsync(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth");

    private async Task AnswerOutsideRequestAsync(IRoute route)
    {
        var url = route.Request.Url;
        if (url.StartsWith(MapTiles, StringComparison.Ordinal))
        {
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "image/png", BodyBytes = _blankTile });
        }
        else if (url.Contains(MapStyles, StringComparison.Ordinal))
        {
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "application/json", Body = BlankStyle });
        }
        else
        {
            _browserErrors.Enqueue($"The page asked another site for {url}");
            await route.AbortAsync();
        }
    }

    private void Watch(IPage page)
    {
        page.Console += (_, message) =>
        {
            if (message.Type == "error") _browserErrors.Enqueue($"{page.Url}: {message.Text}");
        };
        page.PageError += (_, error) => _browserErrors.Enqueue($"{page.Url}: {error}");
    }
}
