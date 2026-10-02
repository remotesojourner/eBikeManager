using System.Globalization;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;

namespace EBikeManager.Application.Utils;

internal static class NotificationEventData
{
    public static object? For(NotificationEvent notificationEvent) => notificationEvent switch
    {
        RideSynced synced => Ride(synced.Ride),
        UploadFailed failed => new { service = failed.Service, error = failed.Error, ride = Ride(failed.Ride) },
        SyncFailed failed => new { error = failed.Error },
        SignInRequired required => new { service = required.Service },
        _ => null
    };

    public static object Ride(RideDto ride) => new
    {
        id = ride.Id,
        title = ride.Title,
        bikeId = ride.BikeId,
        bikeName = ride.BikeName,
        bikeModel = ride.BikeModel,
        startTime = ride.StartTime,
        localStartTime = ride.LocalStartTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        distanceMeters = ride.DistanceMeters,
        movingSeconds = ride.MovingSeconds,
        averageSpeedKmh = ride.AverageSpeedKmh,
        elevationGainMeters = ride.ElevationGainMeters,
        caloriesKcal = ride.CaloriesKcal,
        riderEnergySharePercent = ride.RiderEnergySharePercent
    };
}
