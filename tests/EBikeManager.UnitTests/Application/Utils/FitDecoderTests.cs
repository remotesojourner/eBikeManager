using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Utils;

public class FitDecoderTests
{
    private static readonly DateTime _start = new(2026, 6, 1, 16, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void ReadsTheSessionTotals()
    {
        var summary = FitDecoder.ReadSummary(TestFit.Create(_start, TimeSpan.FromMinutes(75), distanceMeters: 23_456));

        Assert.Equal(_start, summary.StartTime);
        Assert.Equal(_start.AddMinutes(75), summary.EndTime);
        Assert.Equal((75 * 60) - 120, summary.TimerSeconds);
        Assert.Equal(23_456, summary.DistanceMeters);
        Assert.Equal(147, summary.AveragePowerWatts);
        Assert.Equal(1, summary.LapCount);
        Assert.True(summary.HasGps);
    }

    [Fact]
    public void BoschFilesCarryNoCalories()
    {
        Assert.Null(FitDecoder.ReadSummary(TestFit.Create(_start, TimeSpan.FromMinutes(10))).CaloriesKcal);
        Assert.Equal(250, FitDecoder.ReadSummary(TestFit.Create(_start, TimeSpan.FromMinutes(10), calories: 250)).CaloriesKcal);
    }

    [Fact]
    public void NoticesRidesWithoutGps()
    {
        Assert.False(FitDecoder.ReadSummary(TestFit.Create(_start, TimeSpan.FromMinutes(30), withGps: false)).HasGps);
    }

    [Fact]
    public void RejectsDataThatIsNotAFitFile()
    {
        Assert.Throws<InvalidDataException>(() => FitDecoder.ReadSummary("<html>Bad gateway</html>"u8.ToArray()));
    }

    [Fact]
    public void RejectsTruncatedFitFiles()
    {
        var fit = TestFit.Create(_start, TimeSpan.FromMinutes(30));

        Assert.Throws<InvalidDataException>(() => FitDecoder.ReadSummary(fit[..^20]));
    }
}
