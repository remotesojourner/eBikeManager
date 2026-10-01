using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class ApiTokenTests : BrowserTest, IClassFixture<BrowserAppSignInRequired>
{
    private readonly BrowserAppSignInRequired _app;

    public ApiTokenTests(BrowserAppSignInRequired app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TheSecurityTabShowsTheStatisticsTokenAndCanReplaceIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();
        var token = page.GetByLabel("API token", new() { Exact = true });

        await page.GotoAsync("/settings/security");
        await Expect(page.GetByText($"{_app.BaseAddress}api/statistics")).ToBeVisibleAsync();
        await Expect(token).ToHaveValueAsync(new Regex("^ebm_"));
        var first = await token.InputValueAsync();
        Assert.Equal(HttpStatusCode.OK, await StatisticsStatusAsync(first, cancellationToken));

        await page.GetByRole(AriaRole.Button, new() { Name = "Generate a new token" }).ClickAsync();
        await Expect(page.GetByText("New token generated. The old one no longer works.")).ToBeVisibleAsync();
        await Expect(token).Not.ToHaveValueAsync(first);
        var second = await token.InputValueAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, await StatisticsStatusAsync(first, cancellationToken));
        Assert.Equal(HttpStatusCode.OK, await StatisticsStatusAsync(second, cancellationToken));
        AssertNoBrowserErrors();
    }

    private async Task<HttpStatusCode> StatisticsStatusAsync(string token, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { BaseAddress = _app.BaseAddress };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("api/statistics", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }
}
