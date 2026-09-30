namespace EBikeManager.Application.Models;

public sealed record HealthExercise(string Name, string? ExerciseType, string? DisplayName, DateTime StartTime, DateTime EndTime);
