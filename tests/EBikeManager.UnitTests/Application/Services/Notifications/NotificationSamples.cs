using System.Net;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Services.Notifications;

internal static class NotificationSamples
{
    public static RideDto Ride { get; } = new(
        "ride-42", "bike-1", "Commuter", "TENWAYS (Performance Line)", "Westminster to Blackfriars and back",
        new DateTime(2026, 9, 27, 7, 14, 0, DateTimeKind.Utc), new DateTime(2026, 9, 27, 8, 14, 0, DateTimeKind.Unspecified),
        4080, 612, 104, 18, 24.0, 64, 137, BackupStatus.Saved, null, "fit/2026/09/ride-42.fit", BackupStatus.Saved, null);

    public static RideDto UntitledRide { get; } = Ride with { Title = null, DistanceMeters = null, AverageSpeedKmh = null };

    public static InMemoryNotificationChannels Channels(string type, string settings) =>
        new([new NotificationChannel { Id = "abc123", Type = type, DisplayName = type, Data = settings }]);

    public static RecordingHandler Answering(HttpStatusCode status = HttpStatusCode.OK, string body = "") =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });
}
