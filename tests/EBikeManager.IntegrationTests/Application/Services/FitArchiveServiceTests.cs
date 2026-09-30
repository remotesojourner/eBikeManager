using System.Security.Cryptography;
using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Options;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class FitArchiveServiceTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly FitArchiveService _archive;

    public FitArchiveServiceTests()
    {
        _archive = new FitArchiveService(Options.Create(new EBikeManagerOptions { DataDirectory = _folder.Path }));
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void FilesAreNamedAfterTheRidesLocalStartTime()
    {
        var ride = new Ride { Id = "abc/123:x", StartTime = new DateTime(2026, 6, 1, 16, 30, 0, DateTimeKind.Utc), TimeZone = "Europe/London" };

        Assert.Equal("2026/06/2026-06-01_1730_abc_123_x.fit", FitArchiveService.RelativePathFor(ride));
    }

    [Fact]
    public async Task TheFitFileAndBoschSummaryAreSavedSideBySide()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var fit = TestFit.Create(DateTime.UtcNow, TimeSpan.FromMinutes(10));

        var sha = await _archive.SaveAsync("2026/06/ride.fit", fit, """{"title":"Ride"}""", cancellationToken);

        var fitPath = Path.Combine(_folder.Path, "fit", "2026", "06", "ride.fit");
        Assert.Equal(fit, await File.ReadAllBytesAsync(fitPath, cancellationToken));
        Assert.Equal("""{"title":"Ride"}""", await File.ReadAllTextAsync(Path.ChangeExtension(fitPath, ".json"), cancellationToken));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(fit)), sha);
        Assert.Equal(fit, await _archive.ReadAsync("2026/06/ride.fit", cancellationToken));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(fitPath)!, "*.tmp"));
    }

    [Theory]
    [InlineData("../storage.db")]
    [InlineData("2026/../../keys/key.xml")]
    public void PathsOutsideTheArchiveAreRefused(string relativePath)
    {
        Assert.Throws<ArgumentException>(() => _archive.FullPath(relativePath));
    }
}
