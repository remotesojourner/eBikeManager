namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeDetailsDto(
    string Id,
    string Name,
    string? Brand,
    DateTime? PictureSavedAt,
    string? FrameNumber,
    string? FrameNumberPosition,
    double? OdometerMeters,
    double? MotorHours,
    double? MotorHoursAssisted,
    double? MaxAssistSpeedKmh,
    double? WheelCircumferenceMm,
    bool? WalkAssistEnabled,
    bool? LockEnabled,
    bool? AlarmEnabled,
    ServiceDueDto? ServiceDue,
    IReadOnlyList<BikeBatteryDto> Batteries,
    IReadOnlyList<BikeComponentDto> Components,
    IReadOnlyList<AssistModeDto> AssistModes,
    IReadOnlyList<AssistModeMileageDto> ModeMileage,
    BikeLiveStateDto? LiveState,
    BikeLocationDto? LastLocation,
    IReadOnlyList<BikeDocumentDto> Documents,
    bool? HasFlowPlus,
    DateTime? UpdatedAt)
{
    public bool HasDetails => UpdatedAt != null;
}
