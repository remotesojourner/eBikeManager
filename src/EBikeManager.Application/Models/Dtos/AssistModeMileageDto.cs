using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Models.Dtos;

public sealed record AssistModeMileageDto(string Name, string? Color, bool IsOff, double DistanceMeters, double? EnergyWh, double Percent)
{
    private const double MinimumMetresForEfficiency = 1000;

    public double? WattHoursPerKilometre =>
        EnergyWh is { } energy && DistanceMeters >= MinimumMetresForEfficiency ? energy / (DistanceMeters / UnitConversion.MetresPerKilometre) : null;
}
