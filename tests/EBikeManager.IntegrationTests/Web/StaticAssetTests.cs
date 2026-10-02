using System.Text.RegularExpressions;
using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Web;

public sealed partial class StaticAssetTests : IClassFixture<SetUpApp>, IClassFixture<SignInRequiredApp>
{
    private readonly SetUpApp _app;
    private readonly SignInRequiredApp _signInRequired;

    public StaticAssetTests(SetUpApp app, SignInRequiredApp signInRequired)
    {
        _app = app;
        _signInRequired = signInRequired;
    }

    [Fact]
    public async Task PagesLinkTheirStylesAndScriptsByContentSoAnUpgradeIsNeverServedFromAnOldCache()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient();

        var page = await client.GetStringAsync(new Uri("/", UriKind.Relative), cancellationToken);

        Assert.Matches(StylesheetLink(), page);
        Assert.Matches(MapScriptLink(), page);
        Assert.DoesNotContain("href=\"css/ebike-manager.css\"", page, StringComparison.Ordinal);

        using var response = await client.GetAsync(new Uri(StylesheetLink().Match(page).Groups["path"].Value, UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task StylesAndScriptsLoadWithoutSigningIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var setUp = _app.CreateClient();
        var page = await setUp.GetStringAsync(new Uri("/", UriKind.Relative), cancellationToken);
        using var visitor = _signInRequired.CreateClient(new() { AllowAutoRedirect = false });

        using var stylesheet = await visitor.GetAsync(new Uri(StylesheetLink().Match(page).Groups["path"].Value, UriKind.Relative), cancellationToken);
        using var plain = await visitor.GetAsync(new Uri("/css/ebike-manager.css", UriKind.Relative), cancellationToken);
        using var library = await visitor.GetAsync(new Uri("/lib/maplibre-gl/maplibre-gl.js", UriKind.Relative), cancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, stylesheet.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, plain.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, library.StatusCode);
        Assert.True(plain.Headers.CacheControl?.NoCache, "Files asked for by their plain name must be checked again on every load.");
    }

    [GeneratedRegex("href=\"(?<path>css/ebike-manager\\.[a-z0-9]+\\.css)\"")]
    private static partial Regex StylesheetLink();

    [GeneratedRegex("src=\"js/ebike-manager-maps\\.[a-z0-9]+\\.js\"")]
    private static partial Regex MapScriptLink();
}
