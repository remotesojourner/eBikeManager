using System.Collections.Frozen;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.Application.Utils;

public static class BridgeSensors
{
    private const string Prefix = "eBike ";

    private static readonly FrozenDictionary<string, BridgeReading> _readings = new Dictionary<string, BridgeReading>
    {
        ["Battery SoC (Live)"] = BridgeReading.BatteryPercent,
        ["Odometer (Live)"] = BridgeReading.Odometer,
        ["Speed"] = BridgeReading.Speed,
        ["Cadence"] = BridgeReading.Cadence,
        ["Rider Power"] = BridgeReading.RiderPower,
        ["Ambient Brightness"] = BridgeReading.AmbientBrightness,
        ["Charge Time to 80% (Estimate)"] = BridgeReading.ChargeTimeTo80,
        ["Charge Time to 100% (Estimate)"] = BridgeReading.ChargeTimeTo100,
        ["Connected"] = BridgeReading.Connected,
        ["Light"] = BridgeReading.Light,
        ["System Locked"] = BridgeReading.Locked,
        ["Charger Connected"] = BridgeReading.ChargerConnected,
        ["Light Reserve"] = BridgeReading.LightReserve,
        ["Diagnosis Active"] = BridgeReading.DiagnosisActive,
        ["In Motion"] = BridgeReading.InMotion
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static BridgeSensor? Find(string entityName)
    {
        var name = entityName.Trim();
        if (!name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return null;

        var rest = name[Prefix.Length..];
        var slot = 1;
        if (rest is ['1' or '2', ' ', ..])
        {
            slot = rest[0] - '0';
            rest = rest[2..];
        }

        return _readings.TryGetValue(rest, out var reading) ? new BridgeSensor(slot, reading) : null;
    }

    public static BridgeReadingsDto Apply(BridgeReadingsDto readings, BridgeReading reading, float? number, bool? flag) => reading switch
    {
        BridgeReading.BatteryPercent => readings with { BatteryPercent = number },
        BridgeReading.Odometer => readings with { OdometerKm = number },
        BridgeReading.Speed => readings with { SpeedKmh = number },
        BridgeReading.Cadence => readings with { CadenceRpm = number },
        BridgeReading.RiderPower => readings with { RiderPowerWatts = number },
        BridgeReading.AmbientBrightness => readings with { AmbientBrightnessLux = number },
        BridgeReading.ChargeTimeTo80 => readings with { ChargeTimeTo80Minutes = number },
        BridgeReading.ChargeTimeTo100 => readings with { ChargeTimeTo100Minutes = number },
        BridgeReading.Connected => readings with { Connected = flag == true },
        BridgeReading.Light => readings with { LightOn = flag },
        BridgeReading.Locked => readings with { Locked = !flag },
        BridgeReading.ChargerConnected => readings with { ChargerConnected = flag },
        BridgeReading.LightReserve => readings with { LightReserveActive = flag },
        BridgeReading.DiagnosisActive => readings with { DiagnosisActive = flag },
        BridgeReading.InMotion => readings with { InMotion = flag },
        _ => readings
    };
}
