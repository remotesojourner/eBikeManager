namespace EBikeManager.Application.Models;

public sealed record BoschActivityPage(IReadOnlyList<BoschActivity> Activities, int TotalPages);
