using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Utils;

public static class UnitConversion
{
    public const double MetresPerKilometre = 1000;
    public const double MetresPerMile = 1609.344;
    public const double MetresPerFoot = 0.3048;

    public static double Distance(double metres, UnitSystem units) =>
        metres / (units == UnitSystem.Imperial ? MetresPerMile : MetresPerKilometre);

    public static double Speed(double kilometresPerHour, UnitSystem units) =>
        units == UnitSystem.Imperial ? kilometresPerHour * MetresPerKilometre / MetresPerMile : kilometresPerHour;

    public static double ShortDistance(double metres, UnitSystem units) =>
        units == UnitSystem.Imperial ? metres / MetresPerFoot : metres;

    public static double PerDistance(double perKilometre, UnitSystem units) =>
        units == UnitSystem.Imperial ? perKilometre * MetresPerMile / MetresPerKilometre : perKilometre;

    public static double SplitMetres(UnitSystem units) => units == UnitSystem.Imperial ? MetresPerMile : MetresPerKilometre;

    public static string DistanceUnit(UnitSystem units) => units == UnitSystem.Imperial ? "mi" : "km";

    public static string SpeedUnit(UnitSystem units) => units == UnitSystem.Imperial ? "mph" : "km/h";

    public static string ShortDistanceUnit(UnitSystem units) => units == UnitSystem.Imperial ? "ft" : "m";
}
