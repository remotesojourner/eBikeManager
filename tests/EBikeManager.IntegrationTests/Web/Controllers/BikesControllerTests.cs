using System.Net;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;

namespace EBikeManager.IntegrationTests.Web.Controllers;

public sealed class BikesControllerTests : IClassFixture<SetUpApp>, IClassFixture<PasswordProtectedApp>
{
    private static readonly Uri _picture = new($"/bikes/{SampleData.BikeId}/picture?v=1", UriKind.Relative);

    private readonly SetUpApp _app;
    private readonly PasswordProtectedApp _protectedApp;

    public BikesControllerTests(SetUpApp app, PasswordProtectedApp protectedApp)
    {
        _app = app;
        _protectedApp = protectedApp;
    }

    [Fact]
    public async Task TheBikePictureIsServedFromTheAppsOwnCopy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient();
        var downloadsBefore = _app.Bosch.DownloadedPictures.Count;

        using var response = await client.GetAsync(_picture, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(BoschSamples.Picture, await response.Content.ReadAsByteArrayAsync(cancellationToken));
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.Equal(downloadsBefore, _app.Bosch.DownloadedPictures.Count);
    }

    [Fact]
    public async Task ABikeWithoutAPictureIsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient();

        using var response = await client.GetAsync(new Uri("/bikes/unknown/picture", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ThePictureNeedsSigningInWhenAPasswordIsSet()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _protectedApp.CreateClient();

        using var response = await client.GetAsync(_picture, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
