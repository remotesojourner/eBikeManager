namespace EBikeManager.Application.Models.Dtos;

public sealed record BridgeReadingsDto(
    int Slot,
    string? BikeId,
    bool Connected,
    double? BatteryPercent,
    double? OdometerKm,
    double? SpeedKmh,
    double? CadenceRpm,
    double? RiderPowerWatts,
    double? AmbientBrightnessLux,
    double? ChargeTimeTo80Minutes,
    double? ChargeTimeTo100Minutes,
    bool? LightOn,
    bool? Locked,
    bool? ChargerConnected,
    bool? LightReserveActive,
    bool? DiagnosisActive,
    bool? InMotion)
{
    public static BridgeReadingsDto Empty(int slot, string? bikeId) =>
        new(slot, bikeId, false, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
}
