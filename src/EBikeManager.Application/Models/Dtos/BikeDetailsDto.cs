namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeDetailsDto(
    string Id,
    string Name,
    string? Brand,
    DateTime? PictureSavedAt,
    string? FrameNumber,
    double? OdometerMeters,
    double? MotorHours,
    double? MotorHoursAssisted,
    double? MaxAssistSpeedKmh,
    bool? WalkAssistEnabled,
    bool? LockEnabled,
    bool? AlarmEnabled,
    ServiceDueDto? ServiceDue,
    IReadOnlyList<BikeBatteryDto> Batteries,
    IReadOnlyList<BikeComponentDto> Components,
    IReadOnlyList<AssistModeDto> AssistModes,
    BikeLiveStateDto? LiveState,
    BikeLocationDto? LastLocation,
    bool? HasFlowPlus,
    DateTime? UpdatedAt)
{
    public bool HasDetails => UpdatedAt != null;
}
