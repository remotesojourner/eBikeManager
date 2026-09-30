using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Web.Utils;
using MudBlazor;

namespace EBikeManager.UnitTests.Web.Utils;

public class UploadAppearanceTests
{
    [Theory]
    [InlineData(RideExportStatus.Uploaded, null, Color.Success)]
    [InlineData(RideExportStatus.Uploaded, "Google stored it without the route.", Color.Warning)]
    [InlineData(RideExportStatus.Failed, null, Color.Error)]
    [InlineData(RideExportStatus.WatchRecorded, null, Color.Error)]
    public void UploadsWithANoteStandOutFromCleanUploadsAndFailures(RideExportStatus status, string? note, Color expected)
    {
        var export = new RideExportDto("ride-1", "Ride", new DateTime(2026, 9, 30, 9, 0, 0, DateTimeKind.Unspecified), "googleHealth", status, null, note, null);

        Assert.Equal(expected, UploadAppearance.ColorOf(export));
    }
}
