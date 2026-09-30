using System.Net;
using EBikeManager.Application.Services;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Application.Services;

public sealed class GitHubReleaseServiceTests : IDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingHandler _handler;
    private readonly RecordingLogger<GitHubReleaseService> _logger = new();
    private readonly GitHubReleaseService _releases;
    private HttpStatusCode _status = HttpStatusCode.OK;
    private HttpRequestException? _failure;

    public GitHubReleaseServiceTests()
    {
        _handler = new RecordingHandler(_ => _failure != null ? throw _failure : RecordingHandler.Json("""{"tag_name":"v1.4.0"}""", _status));
        _releases = new GitHubReleaseService(new StubHttpClientFactory(_handler), _time, _logger);
    }

    [Fact]
    public async Task TheLatestReleaseOfThisRepositoryIsAsked()
    {
        Assert.Equal("1.4.0", await _releases.GetLatestVersionAsync(TestContext.Current.CancellationToken));

        Assert.Equal(new Uri("https://api.github.com/repos/remotesojourner/eBikeManager/releases/latest"), Assert.Single(_handler.Requests).Uri);
    }

    [Fact]
    public async Task TheAnswerIsReusedForSixHours()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("1.4.0", await _releases.GetLatestVersionAsync(cancellationToken));
        _time.Advance(GitHubReleaseService.AnswerLifetime - TimeSpan.FromMinutes(1));
        Assert.Equal("1.4.0", await _releases.GetLatestVersionAsync(cancellationToken));
        Assert.Single(_handler.Requests);

        _time.Advance(TimeSpan.FromMinutes(1));
        await _releases.GetLatestVersionAsync(cancellationToken);
        Assert.Equal(2, _handler.Requests.Count);
    }

    [Fact]
    public async Task ARefusalIsNotRetriedForAnHourAndTheLastKnownVersionIsKept()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _releases.GetLatestVersionAsync(cancellationToken);
        _time.Advance(GitHubReleaseService.AnswerLifetime);
        _status = HttpStatusCode.Forbidden;

        Assert.Equal("1.4.0", await _releases.GetLatestVersionAsync(cancellationToken));
        _time.Advance(GitHubReleaseService.FailureLifetime - TimeSpan.FromMinutes(1));
        Assert.Equal("1.4.0", await _releases.GetLatestVersionAsync(cancellationToken));

        Assert.Equal(2, _handler.Requests.Count);
        Assert.Single(_logger.Warnings);

        _time.Advance(TimeSpan.FromMinutes(1));
        await _releases.GetLatestVersionAsync(cancellationToken);
        Assert.Equal(3, _handler.Requests.Count);
    }

    [Fact]
    public async Task ANetworkFailureBeforeAnyAnswerGivesNoVersion()
    {
        _failure = new HttpRequestException("No route to host");

        Assert.Null(await _releases.GetLatestVersionAsync(TestContext.Current.CancellationToken));
        Assert.Single(_logger.Warnings);
    }

    [Fact]
    public async Task TabsAskingAtTheSameTimeShareOneRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var answers = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _releases.GetLatestVersionAsync(cancellationToken)));

        Assert.All(answers, answer => Assert.Equal("1.4.0", answer));
        Assert.Single(_handler.Requests);
    }

    public void Dispose() => _releases.Dispose();
}
