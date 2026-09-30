namespace EBikeManager.Application.Models;

public sealed record SyncRunResult(int RidesChecked, int NewRides, int FitFilesSaved, IReadOnlyList<string> Problems);
