using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IGoogleHealthApiService
{
    Task<string> CreateExerciseAsync(ExerciseUpload upload, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthExercise>> ListWatchExercisesAsync(DateTime civilFrom, DateTime civilTo, CancellationToken cancellationToken = default);

    Task RemoveOwnExerciseAsync(string dataPointId, CancellationToken cancellationToken = default);
}
