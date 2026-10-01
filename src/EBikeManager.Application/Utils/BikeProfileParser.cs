using System.Text.Json;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Utils;

public static class BikeProfileParser
{
    private static readonly (string Property, BikeComponentKind Kind)[] _components =
    [
        ("driveUnit", BikeComponentKind.DriveUnit),
        ("headUnit", BikeComponentKind.HeadUnit),
        ("remoteControl", BikeComponentKind.RemoteControl),
        ("connectedModule", BikeComponentKind.ConnectModule),
        ("antiLockBrakeSystem", BikeComponentKind.AntiLockBrakeSystem)
    ];

    private const string OffModeName = "OFF";

    public static BikeDetailsDto Parse(Bike bike) => Parse(bike, new Dictionary<string, AssistModeName>(), null, []);

    public static BikeDetailsDto Parse(Bike bike, IReadOnlyDictionary<string, AssistModeName> modeNames, DateTime? pictureSavedAt, IReadOnlyList<BikeDocumentDto> documents)
    {
        using var profileDocument = Read(bike.ProfileJson);
        using var chargeDocument = Read(bike.StateOfChargeJson);
        using var passDocument = Read(bike.PassJson);
        using var locationDocument = Read(bike.LocationJson);

        var profile = profileDocument?.RootElement ?? default;
        var driveUnit = profile.Property("driveUnit");
        var wheel = driveUnit.Property("rearWheelCircumference");
        var charge = chargeDocument?.RootElement;
        var pass = passDocument?.RootElement ?? default;
        var boschOdometer = charge?.Number("odometer") ?? driveUnit.Number("totalDistanceTraveled");
        var bridgeOdometer = bike.BridgeOdometerKm * 1000;

        return new BikeDetailsDto(
            bike.Id,
            bike.Name,
            bike.Model,
            pictureSavedAt,
            pass.Text("frameNumber") ?? profile.Text("frameNumber"),
            pass.Text("frameNumberPosition"),
            boschOdometer is { } bosch && bridgeOdometer is { } bridge ? Math.Max(bosch, bridge) : boschOdometer ?? bridgeOdometer,
            boschOdometer,
            driveUnit.Property("powerOnTime").Number("total"),
            driveUnit.Property("powerOnTime").Number("withMotorSupport"),
            driveUnit.Number("maxAssistanceSpeed") ?? driveUnit.Property("maximumAssistance").Number("speed"),
            wheel.Number("userValue") ?? wheel.Number("defaultValue"),
            driveUnit.Property("walkAssist").Flag("isEnabled"),
            driveUnit.Property("lock").Flag("isEnabled"),
            profile.Property("connectedModule").Flag("isAlarmFeatureEnabled"),
            ServiceDue(profile.Property("remoteControl").Property("serviceDue")),
            [.. profile.Items("batteries").Select(Battery)],
            [.. _components.Select(component => Component(profile.Property(component.Property), component.Kind)).OfType<BikeComponentDto>()],
            [.. driveUnit.Items("driveUnitAssistModes").Select(mode => AssistMode(mode, modeNames)).OfType<AssistModeDto>().OrderByDescending(mode => mode.ReachableRangeKm ?? 0)],
            ModeMileage(driveUnit, modeNames),
            charge is { } live ? LiveState(live) : null,
            bike.BridgeBatteryPercent,
            bike.BridgeBatteryAt,
            locationDocument?.RootElement is { } location ? Location(location) : null,
            documents,
            bike.HasFlowPlus,
            bike.DetailsUpdatedAt);
    }

    private static JsonDocument? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static BikeBatteryDto Battery(JsonElement battery)
    {
        var cycles = battery.Property("numberOfFullChargeCycles");
        return new BikeBatteryDto(
            battery.Text("productName"),
            battery.Number("totalEnergy"),
            battery.Number("batteryLevel"),
            battery.Number("remainingEnergy"),
            cycles.Number("total"),
            cycles.Number("onBike"),
            cycles.Number("offBike"),
            battery.Number("deliveredWhOverLifetime"),
            battery.Flag("isCharging"),
            battery.Flag("isChargerConnected"),
            battery.Text("softwareVersion"),
            battery.Text("serialNumber"));
    }

    private static BikeComponentDto? Component(JsonElement component, BikeComponentKind kind) =>
        component.ValueKind == JsonValueKind.Object
            ? new BikeComponentDto(kind, component.Text("productName"), component.Text("softwareVersion"), component.Text("serialNumber"), component.Text("partNumber"), component.Text("hardwareVersion"))
            : null;

    private static AssistModeDto? AssistMode(JsonElement mode, IReadOnlyDictionary<string, AssistModeName> modeNames)
    {
        var (name, color, isOff) = Describe(mode, modeNames);
        return name == null || isOff ? null : new AssistModeDto(name, mode.Whole("slot"), mode.Number("reachableRange"), color);
    }

    private static List<AssistModeMileageDto> ModeMileage(JsonElement driveUnit, IReadOnlyDictionary<string, AssistModeName> modeNames)
    {
        var modes = new List<(string Name, string? Color, bool IsOff, double Distance, double? Energy)>();
        foreach (var mode in driveUnit.Items("driveUnitAssistModes"))
        {
            var statistics = mode.Property("statistics");
            if (statistics.Number("distance") is not { } distance) continue;

            var (name, color, isOff) = Describe(mode, modeNames);
            if (name == null || (isOff && distance <= 0)) continue;

            modes.Add((name, isOff ? null : color, isOff, distance, statistics.Number("consumedEnergy")));
        }

        var total = modes.Sum(mode => mode.Distance);
        return [.. modes.Select(mode => new AssistModeMileageDto(mode.Name, mode.Color, mode.IsOff, mode.Distance, mode.Energy, total > 0 ? mode.Distance * 100 / total : 0))];
    }

    private static (string? Name, string? Color, bool IsOff) Describe(JsonElement mode, IReadOnlyDictionary<string, AssistModeName> modeNames)
    {
        var id = mode.Text("id");
        var known = id != null ? modeNames.GetValueOrDefault(id) : null;
        var name = known?.Name ?? mode.Text("longName") ?? mode.Text("shortName") ?? id;
        var isOff = mode.Flag("isOffMode") == true || id == "0" || string.Equals(name, OffModeName, StringComparison.OrdinalIgnoreCase);
        return (isOff && id == name ? OffModeName : name, known?.Color ?? BoschJson.Colour(mode.Number("color")), isOff);
    }

    private static ServiceDueDto? ServiceDue(JsonElement serviceDue)
    {
        var date = serviceDue.Timestamp("date");
        var distance = serviceDue.Number("totalDistance");
        return date == null && distance == null ? null : new ServiceDueDto(date, distance);
    }

    private static BikeLiveStateDto LiveState(JsonElement charge)
    {
        var ranges = charge.Property("reachableRange") switch
        {
            { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.Number).Select(value => value.GetDouble()).Where(value => value > 0).ToList(),
            { ValueKind: JsonValueKind.Number } single => [single.GetDouble()],
            _ => []
        };

        return new BikeLiveStateDto(
            charge.Number("stateOfCharge"),
            charge.Flag("chargingActive"),
            charge.Flag("chargerConnected"),
            charge.Number("remainingEnergyForRider"),
            ranges.Count > 0 ? ranges.Min() : null,
            ranges.Count > 0 ? ranges.Max() : null,
            charge.Timestamp("lastUpdate") ?? charge.Timestamp("timestamp") ?? charge.Timestamp("updatedAt"));
    }

    private static BikeLocationDto? Location(JsonElement location) =>
        location.Number("latitude") is { } latitude && location.Number("longitude") is { } longitude
            ? new BikeLocationDto(latitude, longitude, location.Number("horizontalAccuracy"), location.Timestamp("detectedAt") ?? location.Timestamp("createdAt"))
            : null;
}
